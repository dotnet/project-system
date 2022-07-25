// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.ProjectSystem.Configuration;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IActiveConfiguredProjectsProvider))]
    internal class ActiveConfiguredProjectsProvider : IActiveConfiguredProjectsProvider
    {
        // A project configuration is considered active if its dimensions matches the active solution configuration skipping 
        // any ignored dimensions names (provided by IActiveConfiguredProjectsDimensionProvider instances):
        //
        // For example, given the following cross-targeting project:
        //
        //      -> All known project configurations:
        //
        //          Configuration   Platform    TargetFramework
        //          -------------------------------------------
        //                  Debug |   AnyCPU  |           net45
        //                  Debug |   AnyCPU  |           net46
        //                Release |   AnyCPU  |           net45
        //                Release |   AnyCPU  |           net46
        //
        //      -> Active solution configuration: 
        //
        //                  Debug |   AnyCPU  |           net45
        //
        //      -> Active configurations return by this class:
        //
        //                  Debug |   AnyCPU  |           net45
        //                  Debug |   AnyCPU  |           net46
        //
        // Whereas, given the following non-cross-targeting project:
        //
        //      -> All known project configurations:
        //
        //          Configuration   Platform
        //          ------------------------
        //                  Debug |   AnyCPU
        //                Release |   AnyCPU
        //
        //      -> Active solution configuration: 
        //
        //                  Debug |   AnyCPU
        //
        //      -> Active configurations return by this class:
        //
        //                  Debug |   AnyCPU

        private readonly IUnconfiguredProjectServices _services;
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public ActiveConfiguredProjectsProvider(IUnconfiguredProjectServices services, UnconfiguredProject project)
        {
            _services = services;
            _project = project;

            DimensionProviders = new OrderPrecedenceImportCollection<IActiveConfiguredProjectsDimensionProvider>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IActiveConfiguredProjectsDimensionProvider> DimensionProviders { get; }

        public async Task<ImmutableDictionary<string, ConfiguredProject>?> GetActiveConfiguredProjectsMapAsync()
        {
            ActiveConfiguredObjects<ConfiguredProject>? projects = await GetActiveConfiguredProjectsAsync();

            if (projects?.Objects.IsEmpty != false)
            {
                return null;
            }

            var builder = PooledDictionary<string, ConfiguredProject>.GetInstance();

            bool isCrossTargeting = projects.Objects.All(project => project.ProjectConfiguration.IsCrossTargeting());

            if (isCrossTargeting)
            {
                foreach (ConfiguredProject project in projects.Objects)
                {
                    string targetFramework = project.ProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty];
                    builder.Add(targetFramework, project);
                }
            }
            else
            {
                builder.Add(string.Empty, projects.Objects[0]);
            }

            return builder.ToImmutableDictionaryAndFree();
        }

        public async Task<ActiveConfiguredObjects<ConfiguredProject>?> GetActiveConfiguredProjectsAsync()
        {
            ActiveConfiguredObjects<ProjectConfiguration>? configurations = await GetActiveProjectConfigurationsAsync();

            if (configurations is null)
            {
                return null;
            }

            ImmutableArray<ConfiguredProject>.Builder builder = ImmutableArray.CreateBuilder<ConfiguredProject>(configurations.Objects.Length);

            foreach (ProjectConfiguration configuration in configurations.Objects)
            {
                ConfiguredProject project = await _project.LoadConfiguredProjectAsync(configuration);

                builder.Add(project);
            }

            return new ActiveConfiguredObjects<ConfiguredProject>(builder.MoveToImmutable(), configurations.DimensionNames);
        }

        public async Task<ActiveConfiguredObjects<ProjectConfiguration>?> GetActiveProjectConfigurationsAsync()
        {
            ProjectConfiguration? activeSolutionConfiguration = _services.ActiveConfiguredProjectProvider?.ActiveProjectConfiguration;

            if (activeSolutionConfiguration is null)
            {
                return null;
            }

            IProjectConfigurationsService? projectConfigurationsService = _services.ProjectConfigurationsService;
            Assumes.Present(projectConfigurationsService);

            IImmutableSet<ProjectConfiguration> configurations = await projectConfigurationsService.GetKnownProjectConfigurationsAsync();

            var builder = PooledArray<ProjectConfiguration>.GetInstance();
            IImmutableSet<string> dimensionNames = GetDimensionNames();

            foreach (ProjectConfiguration configuration in configurations)
            {
                if (IsActiveConfigurationCandidate(activeSolutionConfiguration, configuration, dimensionNames))
                {
                    builder.Add(configuration);
                }
            }

            if (builder.Count == 0)
            {   
                // Active config is different to the known configs, 
                // however we should still return it
                builder.Add(activeSolutionConfiguration);
            }

            return new ActiveConfiguredObjects<ProjectConfiguration>(builder.ToImmutableAndFree(), dimensionNames);
        }

        private IImmutableSet<string> GetDimensionNames()
        {
            ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder(StringComparers.ConfigurationDimensionNames);

            foreach (Lazy<IActiveConfiguredProjectsDimensionProvider> dimensionProvider in DimensionProviders)
            {
                builder.Add(dimensionProvider.Value.DimensionName);
            }

            return builder.ToImmutable();
        }

        private static bool IsActiveConfigurationCandidate(ProjectConfiguration activeSolutionConfiguration, ProjectConfiguration configuration, IImmutableSet<string> ignoredDimensionNames)
        {
            foreach ((string dimensionName, string dimensionValue) in activeSolutionConfiguration.Dimensions)
            {
                if (ignoredDimensionNames.Contains(dimensionName))
                {
                    continue;
                }

                if (!configuration.Dimensions.TryGetValue(dimensionName, out string otherDimensionValue) ||
                    !string.Equals(dimensionValue, otherDimensionValue, StringComparisons.ConfigurationDimensionNames))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
