// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    ///     Implementation of <see cref="IAggregateCrossTargetProjectContextProvider"/> that creates an 
    ///     <see cref="AggregateCrossTargetProjectContext"/> based on the unique TargetFramework 
    ///     configurations of an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IAggregateCrossTargetProjectContextProvider))]
    internal class AggregateCrossTargetProjectContextProvider : OnceInitializedOnceDisposed, IAggregateCrossTargetProjectContextProvider
    {
        private readonly object _gate = new object();
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly List<AggregateCrossTargetProjectContext> _contexts = new List<AggregateCrossTargetProjectContext>();
        private readonly IActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public AggregateCrossTargetProjectContextProvider(
            IUnconfiguredProjectCommonServices commonServices,
            IActiveConfiguredProjectsProvider activeConfiguredProjectsProvider,
            ITargetFrameworkProvider targetFrameworkProvider)
            : base(synchronousDisposal: true)
        {
            _commonServices = commonServices;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public async Task<AggregateCrossTargetProjectContext> CreateProjectContextAsync()
        {
            EnsureInitialized();

            AggregateCrossTargetProjectContext context = await CreateProjectContextAsyncCore();
            if (context == null)
            {
                return null;
            }

            lock (_gate)
            {
                _contexts.Add(context);
            }

            return context;
        }

        public void ReleaseProjectContext(AggregateCrossTargetProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            lock (_gate)
            {
                if (!_contexts.Remove(context))
                    throw new ArgumentException("Specified context was not created by this instance, or has already been unregistered.");
            }
        }

        protected override void Initialize()
        {
            _commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;
        }

        protected override void Dispose(bool disposing)
        {
        }

        private Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
        {
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;

            return Task.CompletedTask;
        }

        private async Task<AggregateCrossTargetProjectContext> CreateProjectContextAsyncCore()
        {
            // Get the set of active configured projects ignoring target framework.
#pragma warning disable CS0618 // Type or member is obsolete
            ImmutableDictionary<string, ConfiguredProject> configuredProjectsMap = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync();
#pragma warning restore CS0618 // Type or member is obsolete
            ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
            ImmutableArray<ITargetFramework>.Builder targetFrameworks = ImmutableArray.CreateBuilder<ITargetFramework>(initialCapacity: configuredProjectsMap.Count);
            ITargetFramework activeTargetFramework = TargetFramework.Empty;

            foreach ((string tfm, ConfiguredProject configuredProject) in configuredProjectsMap)
            {
                ProjectProperties projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                ConfigurationGeneral configurationGeneralProperties = await projectProperties.GetConfigurationGeneralPropertiesAsync();
                ITargetFramework targetFramework = await GetTargetFrameworkAsync(tfm, configurationGeneralProperties);

                targetFrameworks.Add(targetFramework);

                if (activeTargetFramework.Equals(TargetFramework.Empty) &&
                    configuredProject.ProjectConfiguration.Equals(activeProjectConfiguration))
                {
                    activeTargetFramework = targetFramework;
                }
            }

            bool isCrossTargeting = !(configuredProjectsMap.Count == 1 && string.IsNullOrEmpty(configuredProjectsMap.First().Key));

            return new AggregateCrossTargetProjectContext(
                isCrossTargeting,
                targetFrameworks.MoveToImmutable(),
                configuredProjectsMap,
                activeTargetFramework,
                _targetFrameworkProvider);
        }

        private async Task<ITargetFramework> GetTargetFrameworkAsync(
            string shortOrFullName,
            ConfigurationGeneral configurationGeneralProperties)
        {
            if (string.IsNullOrEmpty(shortOrFullName))
            {
                object targetObject = await configurationGeneralProperties.TargetFramework.GetValueAsync();

                if (targetObject == null)
                {
                    return TargetFramework.Empty;
                }

                shortOrFullName = targetObject.ToString();
            }

            return _targetFrameworkProvider.GetTargetFramework(shortOrFullName) ?? TargetFramework.Empty;
        }
    }
}
