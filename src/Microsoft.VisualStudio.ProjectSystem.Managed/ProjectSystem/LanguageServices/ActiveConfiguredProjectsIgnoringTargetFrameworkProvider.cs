// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides set of configured projects whose ProjectConfiguration has all dimensions (ignoring the TargetFramework) matching the active VS configuration.
    ///
    ///     For example, for a cross-targeting project with TargetFrameworks = "net45;net46" we have:
    ///     -> All known configurations:
    ///         Debug | AnyCPU | net45
    ///         Debug | AnyCPU | net46
    ///         Release | AnyCPU | net45
    ///         Release | AnyCPU | net46
    ///         
    ///     -> Say, active VS configuration = "Debug | AnyCPU"
    ///       
    ///     -> Active configurations ignoring TargetFramework returned by this provider:
    ///         Debug | AnyCPU | net45
    ///         Debug | AnyCPU | net46
    /// </summary>
    [Export(typeof(ActiveConfiguredProjectsIgnoringTargetFrameworkProvider))]
    internal class ActiveConfiguredProjectsIgnoringTargetFrameworkProvider : OnceInitializedOnceDisposedAsync
    {
        private readonly object _gate = new object();
        private readonly IUnconfiguredProjectServices _services;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;

        // Cache the last queried TargetFrameworks and associated configured projects per target framework map.
        // Read/writes for both these fields must be done within a lock to keep them in sync.
        private ImmutableDictionary<string, ConfiguredProject> _cachedConfiguredProjectsMap;
        private string _cachedTargetFrameworks;

        private IDisposable _evaluationSubscriptionLink;

        [ImportingConstructor]
        public ActiveConfiguredProjectsIgnoringTargetFrameworkProvider(
            IUnconfiguredProjectServices services,
            IUnconfiguredProjectCommonServices commonServices,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(commonServices, nameof(commonServices));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));

            _services = services;
            _commonServices = commonServices;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal async Task OnProjectFactoryCompletedAsync()
        {
            await InitializeCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            using (_tasksService.LoadedProject())
            {
                await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);

                // Listen to changes to "TargetFrameworks" property.
                var watchedEvaluationRules = Empty.OrdinalIgnoreCaseStringSet.Add(ConfigurationGeneral.SchemaName);
                _evaluationSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                        new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(OnProjectChanged),
                        ruleNames: watchedEvaluationRules, suppressVersionOnlyUpdates: true);
            }
        }

        private async Task OnProjectChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            IProjectChangeDescription projectChange = e.Value.ProjectChanges[ConfigurationGeneral.SchemaName];
            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty))
            {
                var targetFrameworks = projectChange.After.Properties[ConfigurationGeneral.TargetFrameworksProperty];
                await GetActiveConfiguredProjectsMapAsync(targetFrameworks).ConfigureAwait(false);
            }
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _evaluationSubscriptionLink?.Dispose();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the active configured projects for the given set of "TargetFrameworks" for a cross-targeting project.
        /// </summary>
        private async Task<ImmutableDictionary<string, ConfiguredProject>> GetActiveConfiguredProjectsMapAsync(string targetFrameworks)
        {
            lock (_gate)
            {
                if (string.Equals(targetFrameworks, _cachedTargetFrameworks, StringComparison.OrdinalIgnoreCase))
                {
                    return _cachedConfiguredProjectsMap;
                }
            }

            var builder = ImmutableDictionary.CreateBuilder<string, ConfiguredProject>();
            var knownConfigurations = await _services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync().ConfigureAwait(true);
            var isCrossTarging = knownConfigurations.All(c => c.IsCrossTargeting());
            if (isCrossTarging)
            {
                var activeConfiguration = _services.ActiveConfiguredProjectProvider.ActiveProjectConfiguration;
                foreach (var configuration in knownConfigurations)
                {
                    var isActiveIgnoringTargetFramework = true;

                    // Get all the project configurations with all dimensions (ignoring the TargetFramework) matching the active configuration.
                    foreach (var dimensionKvp in configuration.Dimensions)
                    {
                        var dimensionName = dimensionKvp.Key;
                        var dimensionValue = dimensionKvp.Value;

                        // Ignore the TargetFramework.
                        if (string.Equals(dimensionName, TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string activeValue;
                        if (!activeConfiguration.Dimensions.TryGetValue(dimensionName, out activeValue) ||
                            !string.Equals(dimensionValue, activeValue, StringComparison.OrdinalIgnoreCase))
                        {
                            isActiveIgnoringTargetFramework = false;
                            break;
                        }
                    }

                    if (isActiveIgnoringTargetFramework)
                    {
                        var configuredProject = await _commonServices.Project.LoadConfiguredProjectAsync(configuration).ConfigureAwait(true);
                        var targetFramework = configuration.Dimensions[TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName];
                        builder.Add(targetFramework, configuredProject);
                    }
                }
            }
            else
            {
                builder.Add(String.Empty, _services.ActiveConfiguredProjectProvider.ActiveConfiguredProject);
            }

            lock (_gate)
            {
                if (!string.Equals(targetFrameworks, _cachedTargetFrameworks, StringComparison.OrdinalIgnoreCase))
                {
                    _cachedConfiguredProjectsMap = builder.ToImmutable();
                    _cachedTargetFrameworks = targetFrameworks;
                }

                return _cachedConfiguredProjectsMap;
            }
        }

        /// <summary>
        /// Gets the active configured projects by target framework for the current value of "TargetFrameworks" for a cross-targeting project.
        /// </summary>
        /// <returns>Map from target framework to active configured project.</returns>
        public async Task<ImmutableDictionary<string, ConfiguredProject>> GetActiveConfiguredProjectsMapAsync()
        {
            var properties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var targetFrameworks = (string)await properties.TargetFrameworks.GetValueAsync().ConfigureAwait(false);
            return await GetActiveConfiguredProjectsMapAsync(targetFrameworks).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the active configured projects for the current value of "TargetFrameworks" for a cross-targeting project.
        /// </summary>
        /// <returns>Set of active configured projects.</returns>
        public async Task<ImmutableArray<ConfiguredProject>> GetActiveConfiguredProjectsAsync()
        {
            var projectMap = await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);
            return projectMap.Values.ToImmutableArray();
        }

        /// <summary>
        /// Gets the active project configurations for the current value of "TargetFrameworks" for a cross-targeting project.
        /// </summary>
        /// <returns>Set of active project configurations.</returns>
        public async Task<ImmutableArray<ProjectConfiguration>> GetActiveProjectConfigurationsAsync()
        {
            var projectMap = await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);
            return projectMap.Values.Select(p => p.ProjectConfiguration).ToImmutableArray();
        }
    }
}
