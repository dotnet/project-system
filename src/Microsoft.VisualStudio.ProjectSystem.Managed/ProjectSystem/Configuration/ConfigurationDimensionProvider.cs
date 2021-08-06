// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    ///     Provides the "Configuration", "Platform" and "TargetFramework" dimensions and their values.
    /// </summary>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [ConfigurationDimensionDescription(DeclaredDimensions.ConfigurationProperty,    isVariantDimension: false)]
    [ConfigurationDimensionDescription(DeclaredDimensions.PlatformProperty,         isVariantDimension: false)]
    [ConfigurationDimensionDescription(DeclaredDimensions.TargetFrameworkProperty,  isVariantDimension: true)]
    internal partial class ConfigurationDimensionProvider : IProjectConfigurationDimensionsProvider5
    {
        // This provider handles two types of configuration styles, including the combination of both:
        //
        // SDK-style ("declared"):
        //
        //      <PropertyGroup>
        //          <Configurations>Debug;Release</Configurations>
        //          <Platforms>AnyCPU;x86</Platforms>
        //          <TargetFrameworks>net45;net46</TargetFrameworks>
        //      </PropertyGroup>
        // 
        // Legacy-style ("implicit"):
        //
        //      <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' " />
        //      <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' " />
        //
        // In SDK-style configuration syntax, there's two modes, single targeting mode and multi-targeting
        // mode. Legacy-style only ever produces single targeting mode. While we could enable sniffing
        // possible TargetFramework values via conditions, doing so would cause us to be out of sync with
        // the MSBuild command-line multiplexing that it performs over the "TargetFrameworks" property,
        // resulting in different builds between it and VS. The latter would set the TargetFramework
        // global property but not the former.
        //
        // In the single targeting mode, two dimensions are provided. For example, given the following:
        //
        //          <Configurations>Debug;Release</Configurations>
        //          <Platforms>AnyCPU;x86</Platforms>
        //
        //      -or-
        //
        //          <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' " />
        //          <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' " />
        //          <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' " />
        //          <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x86' " />
        //
        // The project is considered as providing the following configurations:
        //
        //      Configuration | Platform
        //      ------------------------
        //      Debug         | AnyCPU
        //      Release       | AnyCPU
        //      Debug         | x86
        //      Release       | x86
        //
        //  In multi-targeting mode, three dimensions are provided. For example, given the following:
        //
        //          <Configurations>Debug;Release</Configurations>
        //          <Platforms>AnyCPU;x86</Platforms>
        //          <TargetFrameworks>net45;net46</TargetFrameworks>
        //
        //  The project is considered as providing the following configurations:
        //
        //      Configuration | Platform | TargetFramework
        //      ------------------------------------------
        //      *Debug        | AnyCPU   | net45
        //      *Release      | AnyCPU   | net45
        //      *Debug        | x86      | net45
        //      *Release      | x86      | net45
        //      Debug         | AnyCPU   | net46
        //      Release       | AnyCPU   | net46
        //      Debug         | x86      | net46
        //      Release       | x86      | net46
        //
        // * denotes the configurations provided through the COM and DTE APIs that do not handle
        // more than two dimensions. This includes CPS handshake with the solution.
        //
        // To be able to accurately calculate configurations for a given project, we need to evaluate
        // the project. However, this leads to a chicken and egg problem; we can't evaluate without
        // calculating configurations, and we can't calculate configurations without evaluating.
        //
        // To remedy this situation, CPS has two distinct steps though us for determining the configuration;
        // first it asks us to take a guess at the "default" configuration *without* evaluating. It then
        // loads this configuration and uses it to determine the "real" configurations. If the original guess
        // is right, we only end up loading a single configuration on load. However, if the original guess is
        // wrong, then we end up loading an extra unneeded/wasted configuration. CPS, at time of writing, does
        // not currently unload this configuration and continues to evaluate/process subscription data, so for
        // performance reasons, its important that we guess right most of the time.
        //
        // Guessing methods that must not use evaluation:
        //
        //      GetBestGuessDimensionNames(ImmutableArray<ProjectPropertyElement>)
        //      GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject)
        //      GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject, String)
        //
        // Accurate methods that can use evaluation:
        //
        //      GetProjectConfigurationDimensionsAsync(UnconfiguredProject)
        //      GetDefaultValuesForDimensionsAsync(UnconfiguredProject)
        //

        internal static readonly ImmutableDictionary<string, string> EmptyDimensions = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparers.ConfigurationDimensionNames);
        internal static readonly ImmutableArray<DimensionDefinition> KnownDimensions = ImmutableArray.Create(
            new DimensionDefinition(DeclaredDimensions.ConfigurationProperty,   DeclaredDimensions.ConfigurationsProperty,     IsVariantDimension: false,      DefaultValue: "Debug"),
            new DimensionDefinition(DeclaredDimensions.PlatformProperty,        DeclaredDimensions.PlatformsProperty,          IsVariantDimension: false,      DefaultValue: "AnyCPU"),
            new DimensionDefinition(DeclaredDimensions.TargetFrameworkProperty, DeclaredDimensions.TargetFrameworksProperty,   IsVariantDimension: true,       DefaultValue: null));

        private readonly IProjectAccessor _projectAccessor;
        private readonly ITelemetryService _telemetryService;

        [ImportingConstructor]
        public ConfigurationDimensionProvider(IProjectAccessor projectAccessor, ITelemetryService telemetryService)
        {
            _projectAccessor = projectAccessor;
            _telemetryService = telemetryService;
        }

        public IEnumerable<string> GetBestGuessDimensionNames(ImmutableArray<ProjectPropertyElement> properties)
        {
            IDimensionValues[] values = GuessDimensionsDefaultValue(properties);

            PostTelemetry(TelemetryEventName.GuessConfigurations, values, outputCount: false);

            return ToDimensions(values);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            return GetBestGuessDefaultValuesForDimensionsAsync(project, EmptyDimensions);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject project, string solutionConfiguration)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNullOrEmpty(solutionConfiguration, nameof(solutionConfiguration));

            return GetBestGuessDefaultValuesForDimensionsAsync(project, ParseSolutionConfiguration(solutionConfiguration));
        }

        private async Task<IEnumerable<KeyValuePair<string, string>>> GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject project, IImmutableDictionary<string, string> solutionConfiguration)
        {
            IDimensionValues[] values = await _projectAccessor.OpenProjectXmlForReadAsync(project,
                projectXml => GuessDimensionsDefaultValue(projectXml.PropertyGroups, solutionConfiguration));

            PostTelemetry(TelemetryEventName.GuessDefaultConfigurationValues, values, outputCount: false);

            return ToKeyValuePairDimensions<string>(values);
        }

        public async Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetProjectConfigurationDimensionsAsync(UnconfiguredProject project)
        {
            IDimensionValues[] values = await GetProjectConfigurationDimensionsCoreAsync(project);

            PostTelemetry(TelemetryEventName.CalculateConfigurations, values);

            return ToKeyValuePairDimensions<IEnumerable<string>>(values);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultValuesForDimensionsAsync(UnconfiguredProject project)
        {
            IDimensionValues[] values = await GetProjectConfigurationDimensionsCoreAsync(project);

            PostTelemetry(TelemetryEventName.CalculateDefaultConfigurationValues, values);

            return ToKeyValuePairDimensions<string>(values);
        }

        public async Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            DimensionDefinition? definition = KnownDimensions.FirstOrDefault(
                d => StringComparers.ConfigurationDimensionNames.Equals(args.DimensionName, d.Name));

            if (definition == null)
                return; // Not one of ours

            // We handle Add/Delete in "Before" stage and "Rename" in After when conditions have already been fixed up
            if (args.Stage == ChangeEventStage.Before && args.Change == ConfigurationDimensionChange.Rename)
                return;

            if (args.Stage == ChangeEventStage.After && args.Change != ConfigurationDimensionChange.Rename)
                return;

            ConfiguredProject configuredProject = (await args.Project.GetSuggestedConfiguredProjectAsync())!;

            await _projectAccessor.OpenProjectForUpgradeableReadAsync(configuredProject, async evaluationProject =>
            {
                IDimensionValuesWithDimension values = await BuildDimensionValuesAsync(configuredProject, evaluationProject, definition);

                DimensionSource source = values.Source;

                if (source == DimensionSource.Implicit && args.Change != ConfigurationDimensionChange.Add)
                    return; // We only ever handle "add" of implicits, the config service handles the rest

                await _projectAccessor.EnterWriteLockAsync((_, _) =>
                {
                    return args.Change switch
                    {
                        ConfigurationDimensionChange.Add when source == DimensionSource.Implicit =>
                            AddImplicitDimensionValueAsync(args.Project, values, args.DimensionValue),

                        ConfigurationDimensionChange.Add => 
                            AddDeclaredDimensionValueAsync(values, args.DimensionValue),

                        ConfigurationDimensionChange.Delete => 
                            RemoveDeclaredDimensionValueAsync(values, args.DimensionValue),

                        ConfigurationDimensionChange.Rename => 
                            RenameDeclaredDimensionValueAsync(values, args.OldDimensionValue!, args.DimensionValue),

                        _ => Assumes.NotReachable<Task>(),
                    };
                });
            });
        }

        private async Task<IDimensionValues[]> GetProjectConfigurationDimensionsCoreAsync(UnconfiguredProject project)
        {
            ConfiguredProject configuredProject = (await project.GetSuggestedConfiguredProjectAsync())!;

            return await _projectAccessor.OpenProjectForReadAsync(configuredProject,
                evaluationProject => BuildDimensionValuesAsync(configuredProject, evaluationProject, KnownDimensions));
        }

        private static IEnumerable<KeyValuePair<string, T>> ToKeyValuePairDimensions<T>(IDimensionValues[] values)
        {
            bool firstValue = typeof(T) == typeof(string);
            return values.Where(value => value.Source != DimensionSource.NotFound)
                         .Select(value => new KeyValuePair<string, T>(value.Definition.Name, firstValue ? (T)(object)value.FirstValue! : (T)value.Values!))
                         .ToArray();
        }
        private static IEnumerable<string> ToDimensions(IDimensionValues[] values)
        {
            return values.Where(value => value.Source != DimensionSource.NotFound)
                         .Select(value => value.Definition.Name)
                         .ToArray();
        }

        private void PostTelemetry(string eventName, IDimensionValues[] values, bool outputCount = true)
        {
            var properties = new List<(string propertyName, object propertyValue)>(values.Length * 2);

            foreach (IDimensionValues dimension in values)
            {
                properties.Add((TelemetryPropertyName.ConfigurationDimensionSource(dimension.Definition.Name), dimension.Source.ToString()));

                if (dimension.Source != DimensionSource.NotFound)
                {
                    Assumes.NotNull(dimension.Values);

                    properties.Add((TelemetryPropertyName.ConfigurationDimensionValues(dimension.Definition.Name), HashValues(dimension.Values, out int count)));

                    if (outputCount)
                    {
                        properties.Add((TelemetryPropertyName.ConfigurationDimensionValuesCount(dimension.Definition.Name), count));
                    }
                }
            }

            _telemetryService.PostProperties(eventName, properties);
        }

        private string HashValues(IEnumerable<string> values, out int count)
        {
            var builder = PooledStringBuilder.GetInstance();

            count = 0;

            foreach (string value in values)
            {
                if (count != 0)
                    builder.Append(";");

                builder.Append(_telemetryService.HashValue(value));
                count++;
            }

            return builder.ToStringAndFree();
        }

        /// <summary>
        ///     Parses a solution configuration in the syntax "Debug|AnyCPU" into <see cref="IImmutableDictionary{TKey, TValue}"/>.
        /// </summary>

        private static IImmutableDictionary<string, string> ParseSolutionConfiguration(string solutionConfiguration)
        {
            Requires.NotNullOrEmpty(solutionConfiguration, nameof(solutionConfiguration));

            ImmutableDictionary<string, string> config = EmptyDimensions;

            foreach (string value in new LazyStringSplit(solutionConfiguration, '|'))
            {
                if (!config.ContainsKey(ConfigurationGeneral.ConfigurationProperty))
                {
                    config = config.Add(ConfigurationGeneral.ConfigurationProperty, value);
                    continue;
                }
                else if (!config.ContainsKey(ConfigurationGeneral.PlatformProperty))
                {
                    // TODO: May need to convert "Any CPU" -> "AnyCPU"
                    config = config.Add(ConfigurationGeneral.PlatformProperty, value);
                }

                break;
            }

            return config;
        }

        // For unit testing
        protected virtual ProjectProperties GetProjectProperties(ConfiguredProject project)
        {
            return project.Services.ExportProvider.GetExportedValue<ProjectProperties>();
        }
    }
}
