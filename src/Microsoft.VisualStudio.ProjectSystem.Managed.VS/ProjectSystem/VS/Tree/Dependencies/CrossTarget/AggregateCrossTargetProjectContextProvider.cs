// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    ///     Implementation of <see cref="IAggregateCrossTargetProjectContextProvider"/> that creates an 
    ///     <see cref="AggregateCrossTargetProjectContext"/> based on the unique TargetFramework 
    ///     configurations of an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IAggregateCrossTargetProjectContextProvider))]
    internal class AggregateCrossTargetProjectContextProvider : OnceInitializedOnceDisposedAsync, IAggregateCrossTargetProjectContextProvider
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly List<AggregateCrossTargetProjectContext> _contexts = new List<AggregateCrossTargetProjectContext>();
        private readonly IActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;
        private readonly Dictionary<ConfiguredProject, ITargetedProjectContext> _configuredProjectContextsMap = new Dictionary<ConfiguredProject, ITargetedProjectContext>();
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public AggregateCrossTargetProjectContextProvider(
            IUnconfiguredProjectCommonServices commonServices,
            IActiveConfiguredProjectsProvider activeConfiguredProjectsProvider,
            ITargetFrameworkProvider targetFrameworkProvider)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _commonServices = commonServices;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public async Task<AggregateCrossTargetProjectContext> CreateProjectContextAsync()
        {
            await EnsureInitialized();

            AggregateCrossTargetProjectContext context = await CreateProjectContextAsyncCore();
            if (context == null)
            {
                return null;
            }

            await ExecuteWithinLockAsync(() =>
            {
                // There's a race here, by the time we've created the project context,
                // the project could have been renamed, handle this.
                ProjectData projectData = GetProjectData();

                context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                _contexts.Add(context);
            });

            return context;
        }

        public void ReleaseProjectContext(AggregateCrossTargetProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            using (_gate.DisposableWait())
            {
                if (!_contexts.Remove(context))
                    throw new ArgumentException("Specified context was not created by this instance, or has already been unregistered.");

                // Update the maps storing configured project host objects and project contexts which are shared across created contexts.
                // We can remove the ones which are only used by the current context being released.
                RemoveUnusedConfiguredProjectsState_NoLock();
            }
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
            foreach (AggregateCrossTargetProjectContext context in _contexts)
            {
                foreach (ConfiguredProject configuredProject in context.InnerConfiguredProjects)
                {
                    unusedConfiguredProjects.Remove(configuredProject);
                }
            }

            foreach (ConfiguredProject configuredProject in unusedConfiguredProjects)
            {
                _configuredProjectContextsMap.Remove(configuredProject);
            }
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _commonServices.Project.ProjectRenamed += OnProjectRenamed;
            _commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;
            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _gate.Dispose();
            return Task.CompletedTask;
        }

        private Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
        {
            _commonServices.Project.ProjectRenamed -= OnProjectRenamed;
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;

            return Task.CompletedTask;
        }

        private Task OnProjectRenamed(object sender, ProjectRenamedEventArgs args)
        {
            return ExecuteWithinLockAsync(() =>
            {
                ProjectData projectData = GetProjectData();

                foreach (AggregateCrossTargetProjectContext context in _contexts)
                {
                    context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                }
            });
        }

        private ProjectData GetProjectData()
        {
            string filePath = _commonServices.Project.FullPath;

            return new ProjectData()
            {
                FullPath = filePath,
                DisplayName = Path.GetFileNameWithoutExtension(filePath)
            };
        }

        private bool TryGetConfiguredProjectState(ConfiguredProject configuredProject, out ITargetedProjectContext targetedProjectContext)
        {
            using (_gate.DisposableWait())
            {
                return _configuredProjectContextsMap.TryGetValue(configuredProject, out targetedProjectContext);
            }
        }

        private void AddConfiguredProjectState(ConfiguredProject configuredProject, ITargetedProjectContext projectContext)
        {
            using (_gate.DisposableWait())
            {
                _configuredProjectContextsMap.Add(configuredProject, projectContext);
            }
        }

        private async Task<AggregateCrossTargetProjectContext> CreateProjectContextAsyncCore()
        {
            ProjectData projectData = GetProjectData();

            // Get the set of active configured projects ignoring target framework.
#pragma warning disable CS0618 // Type or member is obsolete
            ImmutableDictionary<string, ConfiguredProject> configuredProjectsMap = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync();
#pragma warning restore CS0618 // Type or member is obsolete
            ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
            ImmutableDictionary<ITargetFramework, ITargetedProjectContext>.Builder innerProjectContextsBuilder = ImmutableDictionary.CreateBuilder<ITargetFramework, ITargetedProjectContext>();
            ITargetFramework activeTargetFramework = TargetFramework.Empty;

            foreach ((string tfm, ConfiguredProject configuredProject) in configuredProjectsMap)
            {
                ProjectProperties projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                ConfigurationGeneral configurationGeneralProperties = await projectProperties.GetConfigurationGeneralPropertiesAsync();
                ITargetFramework targetFramework = await GetTargetFrameworkAsync(tfm, configurationGeneralProperties);

                if (!TryGetConfiguredProjectState(configuredProject, out ITargetedProjectContext targetedProjectContext))
                {
                    // Get the target path for the configured project.
                    string targetPath = (string)await configurationGeneralProperties.TargetPath.GetValueAsync();
                    string displayName = GetDisplayName(configuredProject, projectData, targetFramework.FullName);

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

            bool isCrossTargeting = !(configuredProjectsMap.Count == 1 && string.IsNullOrEmpty(configuredProjectsMap.First().Key));
            return new AggregateCrossTargetProjectContext(isCrossTargeting,
                                                            innerProjectContextsBuilder.ToImmutable(),
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

        private Task ExecuteWithinLockAsync(Action action) => _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, action);

        private int _isInitialized;

        private async Task EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
            {
                await InitializeAsync();
            }
        }

        private struct ProjectData
        {
            public string FullPath;
            public string DisplayName;
        }
    }
}
