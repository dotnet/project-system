// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    ///     Implementation of <see cref="IAggregateCrossTargetProjectContextProvider"/> that creates an 
    ///     <see cref="AggregateCrossTargetProjectContext"/> based on the unique TargetFramework 
    ///     configurations of an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IAggregateCrossTargetProjectContextProvider))]
    internal partial class AggregateCrossTargetProjectContextProvider : OnceInitializedOnceDisposed, IAggregateCrossTargetProjectContextProvider
    {
        private readonly object _gate = new object();
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly IProjectAsyncLoadDashboard _asyncLoadDashboard;
        private readonly ITaskScheduler _taskScheduler;
        private readonly List<AggregateCrossTargetProjectContext> _contexts = new List<AggregateCrossTargetProjectContext>();
        private readonly IActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;
        private readonly Dictionary<ConfiguredProject, ITargetedProjectContext> _configuredProjectContextsMap = new Dictionary<ConfiguredProject, ITargetedProjectContext>();
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public AggregateCrossTargetProjectContextProvider(
            IUnconfiguredProjectCommonServices commonServices,
            IProjectAsyncLoadDashboard asyncLoadDashboard,
            ITaskScheduler taskScheduler,
            IActiveConfiguredProjectsProvider activeConfiguredProjectsProvider,
            ITargetFrameworkProvider targetFrameworkProvider)
        {
            Requires.NotNull(commonServices, nameof(commonServices));
            Requires.NotNull(asyncLoadDashboard, nameof(asyncLoadDashboard));
            Requires.NotNull(taskScheduler, nameof(taskScheduler));
            Requires.NotNull(activeConfiguredProjectsProvider, nameof(activeConfiguredProjectsProvider));
            Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));

            _commonServices = commonServices;
            _asyncLoadDashboard = asyncLoadDashboard;
            _taskScheduler = taskScheduler;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public async Task<AggregateCrossTargetProjectContext> CreateProjectContextAsync()
        {
            EnsureInitialized();

            var context = await CreateProjectContextAsyncCore().ConfigureAwait(false);
            if (context == null)
            {
                return null;
            }

            lock (_gate)
            {
                // There's a race here, by the time we've created the project context,
                // the project could have been renamed, handle this.
                var projectData = GetProjectData();

                context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                _contexts.Add(context);
            }

            return context;
        }

        public async Task ReleaseProjectContextAsync(AggregateCrossTargetProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            ImmutableHashSet<ITargetedProjectContext> usedProjectContexts;
            lock (_gate)
            {
                if (!_contexts.Remove(context))
                    throw new ArgumentException("Specified context was not created by this instance, or has already been unregistered.");

                // Update the maps storing configured project host objects and project contexts which are shared across created contexts.
                // We can remove the ones which are only used by the current context being released.
                RemoveUnusedConfiguredProjectsState_NoLock();

                usedProjectContexts = _configuredProjectContextsMap.Values.ToImmutableHashSet();
            }

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            await _commonServices.ThreadingService.SwitchToUIThread();

            // We don't want to dispose the inner workspace contexts that are still being used by other active aggregate contexts.
            Func<ITargetedProjectContext, bool> shouldDisposeInnerContext = c => !usedProjectContexts.Contains(c);

            context.Dispose(shouldDisposeInnerContext);
        }

        // Clears saved host objects and project contexts for unused configured projects.
        private void RemoveUnusedConfiguredProjectsState_NoLock()
        {
            if (_contexts.Count == 0)
            {
                // No active project contexts, clear all state.
                _configuredProjectContextsMap.Clear();
                return;
            }

            var unusedConfiguredProjects = new HashSet<ConfiguredProject>(_configuredProjectContextsMap.Keys);
            foreach (var context in _contexts)
            {
                foreach (var configuredProject in context.InnerConfiguredProjects)
                {
                    unusedConfiguredProjects.Remove(configuredProject);
                }
            }

            foreach (var configuredProject in unusedConfiguredProjects)
            {
                _configuredProjectContextsMap.Remove(configuredProject);
            }
        }

        protected override void Initialize()
        {
            _commonServices.Project.ProjectRenamed += OnProjectRenamed;
            _commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;
        }

        protected override void Dispose(bool disposing)
        {
        }

        private Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
        {
            _commonServices.Project.ProjectRenamed -= OnProjectRenamed;
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;

            return Task.CompletedTask;
        }

        private Task OnProjectRenamed(object sender, ProjectRenamedEventArgs args)
        {
            lock (_gate)
            {
                var projectData = GetProjectData();

                foreach (var context in _contexts)
                {
                    context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                }
            }

            return Task.CompletedTask;
        }
      

        private async Task<string> GetTargetPathAsync()
        {
            ConfigurationGeneral properties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                                     .ConfigureAwait(false);
            return (string)await properties.TargetPath.GetValueAsync()
                                                      .ConfigureAwait(false);
        }

        private ProjectData GetProjectData()
        {
            string filePath = _commonServices.Project.FullPath;

            return new ProjectData() {
                FullPath = filePath,
                DisplayName = Path.GetFileNameWithoutExtension(filePath)
            };
        }

        private bool TryGetConfiguredProjectState(ConfiguredProject configuredProject, out ITargetedProjectContext targetedProjectContext)
        {
            lock (_gate)
            {
                if (_configuredProjectContextsMap.TryGetValue(configuredProject, out targetedProjectContext))
                {
                    return true;
                }
                else
                {
                    targetedProjectContext = null;
                    return false;
                }
            }
        }

        private void AddConfiguredProjectState(ConfiguredProject configuredProject, ITargetedProjectContext projectContext)
        {
            lock (_gate)
            {
                _configuredProjectContextsMap.Add(configuredProject, projectContext);
            }
        }

        private async Task<AggregateCrossTargetProjectContext> CreateProjectContextAsyncCore()
        {           
            // Don't initialize until the project has been loaded into the IDE and available in Solution Explorer
            await _asyncLoadDashboard.ProjectLoadedInHostWithCancellation(_commonServices.Project).ConfigureAwait(false);

            return await _taskScheduler.RunAsync(TaskSchedulerPriority.UIThreadBackgroundPriority, async () => 
            {
                var projectData = GetProjectData();

                // Get the set of active configured projects ignoring target framework.
#pragma warning disable CS0618 // Type or member is obsolete
                var configuredProjectsMap = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync().ConfigureAwait(true);
#pragma warning restore CS0618 // Type or member is obsolete
                var activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                var innerProjectContextsBuilder = ImmutableDictionary.CreateBuilder<ITargetFramework, ITargetedProjectContext>();
                var activeTargetFramework = TargetFramework.Empty;

                foreach (var kvp in configuredProjectsMap)
                {
                    var configuredProject = kvp.Value;
                    var projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                    var configurationGeneralProperties = await projectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    var targetFramework = await GetTargetFrameworkAsync(kvp.Key, configurationGeneralProperties).ConfigureAwait(false);

                    if (!TryGetConfiguredProjectState(configuredProject, out ITargetedProjectContext targetedProjectContext))
                    {
                        // Get the target path for the configured project.
                        var targetPath = (string)await configurationGeneralProperties.TargetPath.GetValueAsync().ConfigureAwait(true);
                        var displayName = GetDisplayName(configuredProject, projectData, targetFramework.Moniker);

                        targetedProjectContext = new TargetedProjectContext(targetFramework, projectData.FullPath, displayName, targetPath)
                        {
                            // By default, set "LastDesignTimeBuildSucceeded = false" until first design time 
                            // build succeeds for this project.
                            LastDesignTimeBuildSucceeded = false
                        };
                        AddConfiguredProjectState(configuredProject, targetedProjectContext);                        
                    }

                    innerProjectContextsBuilder.Add(targetFramework, targetedProjectContext);

                    if (activeTargetFramework.Equals(TargetFramework.Empty) && 
                        configuredProject.ProjectConfiguration.Equals(activeProjectConfiguration))
                    {
                        activeTargetFramework = targetFramework;
                    }
                }

                var isCrossTargeting = !(configuredProjectsMap.Count == 1 && string.IsNullOrEmpty(configuredProjectsMap.First().Key));
                return new AggregateCrossTargetProjectContext(isCrossTargeting,
                                                              innerProjectContextsBuilder.ToImmutable(), 
                                                              configuredProjectsMap, 
                                                              activeTargetFramework,
                                                              _targetFrameworkProvider);
            });
        }

        private async Task<ITargetFramework> GetTargetFrameworkAsync(
            string shortOrFullName, 
            ConfigurationGeneral configurationGeneralProperties)
        {
            if (string.IsNullOrEmpty(shortOrFullName))
            {
                var targetObject = await configurationGeneralProperties.TargetFramework.GetValueAsync().ConfigureAwait(false);
                if (targetObject == null)
                {
                    return TargetFramework.Empty;
                }
                else
                {
                    shortOrFullName = targetObject.ToString();
                }
            }

            return _targetFrameworkProvider.GetTargetFramework(shortOrFullName) ?? TargetFramework.Empty;
        }

        private static string GetDisplayName(ConfiguredProject configuredProject, ProjectData projectData, string targetFramework)
        {
            // For cross targeting projects, we need to ensure that the display name is unique per every target framework.
            // This is needed for couple of reasons:
            //   (a) The display name is used in the editor project context combo box when opening source files that used by more than one inner projects.
            //   (b) Language service requires each active workspace project context in the current workspace to have a unique value for {ProjectFilePath, DisplayName}.
            return configuredProject.ProjectConfiguration.IsCrossTargeting() ?
                $"{projectData.DisplayName}({targetFramework})" :
                projectData.DisplayName;
        }

        private struct ProjectData
        {
            public string FullPath;
            public string DisplayName;
        }
    }
}
