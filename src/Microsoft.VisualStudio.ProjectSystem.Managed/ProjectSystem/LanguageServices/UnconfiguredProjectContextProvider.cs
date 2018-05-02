// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Implementation of <see cref="IProjectContextProvider"/> that creates an <see cref="AggregateWorkspaceProjectContext"/>
    ///     based on the unique TargetFramework configurations of an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IProjectContextProvider))]
    internal partial class UnconfiguredProjectContextProvider : OnceInitializedOnceDisposed, IProjectContextProvider
    {
        private readonly object _gate = new object();
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IWorkspaceProjectContextFactory> _contextFactory;
        private readonly IProjectAsyncLoadDashboard _asyncLoadDashboard;
        private readonly ITaskScheduler _taskScheduler;
        private readonly List<AggregateWorkspaceProjectContext> _contexts = new List<AggregateWorkspaceProjectContext>();
        private readonly IProjectHostProvider _projectHostProvider;
        private readonly IActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly IUnconfiguredProjectHostObject _unconfiguredProjectHostObject;
        private readonly Dictionary<ConfiguredProject, IWorkspaceProjectContext> _configuredProjectContextsMap = new Dictionary<ConfiguredProject, IWorkspaceProjectContext>();
        private readonly Dictionary<ConfiguredProject, IConfiguredProjectHostObject> _configuredProjectHostObjectsMap = new Dictionary<ConfiguredProject, IConfiguredProjectHostObject>();

        [ImportingConstructor]
        public UnconfiguredProjectContextProvider(IUnconfiguredProjectCommonServices commonServices,
                                                 Lazy<IWorkspaceProjectContextFactory> contextFactory,
                                                 IProjectAsyncLoadDashboard asyncLoadDashboard,
                                                 ITaskScheduler taskScheduler,
                                                 IProjectHostProvider projectHostProvider,
                                                 IActiveConfiguredProjectsProvider activeConfiguredProjectsProvider,
                                                 ISafeProjectGuidService projectGuidService)
        {
            _commonServices = commonServices;
            _contextFactory = contextFactory;
            _asyncLoadDashboard = asyncLoadDashboard;
            _taskScheduler = taskScheduler;
            _projectHostProvider = projectHostProvider;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
            _projectGuidService = projectGuidService;
            _unconfiguredProjectHostObject = _projectHostProvider.UnconfiguredProjectHostObject;
        }

        public async Task<AggregateWorkspaceProjectContext> CreateProjectContextAsync()
        {
            EnsureInitialized();

            AggregateWorkspaceProjectContext context = await CreateProjectContextAsyncCore().ConfigureAwait(false);
            if (context == null)
                return null;

            lock (_gate)
            {
                // There's a race here, by the time we've created the project context,
                // the project could have been renamed, handle this.
                ProjectData projectData = GetProjectData();

                context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                _contexts.Add(context);
            }

            return context;
        }

        public async Task ReleaseProjectContextAsync(AggregateWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            ImmutableHashSet<IWorkspaceProjectContext> usedProjectContexts;
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
            bool shouldDisposeInnerContext(IWorkspaceProjectContext c) => !usedProjectContexts.Contains(c);

            context.Dispose(shouldDisposeInnerContext);
        }

        // Clears saved host objects and project contexts for unused configured projects.
        private void RemoveUnusedConfiguredProjectsState_NoLock()
        {
            if (_contexts.Count == 0)
            {
                // No active project contexts, clear all state.
                _configuredProjectContextsMap.Clear();
                _configuredProjectHostObjectsMap.Clear();
                return;
            }

            var unusedConfiguredProjects = new HashSet<ConfiguredProject>(_configuredProjectContextsMap.Keys);
            foreach (AggregateWorkspaceProjectContext context in _contexts)
            {
                foreach (ConfiguredProject configuredProject in context.InnerConfiguredProjects)
                {
                    unusedConfiguredProjects.Remove(configuredProject);
                }
            }

            foreach (ConfiguredProject configuredProject in unusedConfiguredProjects)
            {
                _configuredProjectContextsMap.Remove(configuredProject);
                _configuredProjectHostObjectsMap.Remove(configuredProject);
            }
        }

        protected override void Initialize()
        {
            _commonServices.Project.ProjectRenamed += OnProjectRenamed;
        }

        protected override void Dispose(bool disposing)
        {
            _commonServices.Project.ProjectRenamed -= OnProjectRenamed;
            _unconfiguredProjectHostObject.Dispose();
        }

        private Task OnProjectRenamed(object sender, ProjectRenamedEventArgs args)
        {
            lock (_gate)
            {
                ProjectData projectData = GetProjectData();

                foreach (AggregateWorkspaceProjectContext context in _contexts)
                {
                    context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                }
            }

            return Task.CompletedTask;
        }

        // Returns the name that is the handshake between Roslyn and the csproj/vbproj
        private async Task<string> GetLanguageServiceName()
        {
            ConfigurationGeneral properties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                                .ConfigureAwait(false);

            return (string)await properties.LanguageServiceName.GetValueAsync()
                                                               .ConfigureAwait(false);
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

            return new ProjectData()
            {
                FullPath = filePath,
                DisplayName = Path.GetFileNameWithoutExtension(filePath)
            };
        }

        private bool TryGetConfiguredProjectState(ConfiguredProject configuredProject, out IWorkspaceProjectContext workspaceProjectContext, out IConfiguredProjectHostObject configuredProjectHostObject)
        {
            lock (_gate)
            {
                if (_configuredProjectContextsMap.TryGetValue(configuredProject, out workspaceProjectContext))
                {
                    configuredProjectHostObject = _configuredProjectHostObjectsMap[configuredProject];
                    return true;
                }
                else
                {
                    workspaceProjectContext = null;
                    configuredProjectHostObject = null;
                    return false;
                }
            }
        }

        private void AddConfiguredProjectState(ConfiguredProject configuredProject, IWorkspaceProjectContext workspaceProjectContext, IConfiguredProjectHostObject configuredProjectHostObject)
        {
            lock (_gate)
            {
                _configuredProjectContextsMap.Add(configuredProject, workspaceProjectContext);
                _configuredProjectHostObjectsMap.Add(configuredProject, configuredProjectHostObject);
            }
        }

        private async Task<AggregateWorkspaceProjectContext> CreateProjectContextAsyncCore()
        {
            string languageName = await GetLanguageServiceName().ConfigureAwait(false);
            if (string.IsNullOrEmpty(languageName))
                return null;

            Guid projectGuid = await _projectGuidService.GetProjectGuidAsync()
                                                        .ConfigureAwait(false);

            string targetPath = await GetTargetPathAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(targetPath))
                return null;

            // Don't initialize until the project has been loaded into the IDE and available in Solution Explorer
            await _asyncLoadDashboard.ProjectLoadedInHostWithCancellation(_commonServices.Project).ConfigureAwait(false);

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            return await _taskScheduler.RunAsync(TaskSchedulerPriority.UIThreadBackgroundPriority, async () =>
            {
                await _commonServices.ThreadingService.SwitchToUIThread();

                ProjectData projectData = GetProjectData();

                // Get the set of active configured projects ignoring target framework.
#pragma warning disable CS0618 // Type or member is obsolete
                ImmutableDictionary<string, ConfiguredProject> configuredProjectsMap = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync().ConfigureAwait(true);
#pragma warning restore CS0618 // Type or member is obsolete

                // Get the unconfigured project host object (shared host object).
                var configuredProjectsToRemove = new HashSet<ConfiguredProject>(_configuredProjectHostObjectsMap.Keys);
                ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;

                ImmutableDictionary<string, IWorkspaceProjectContext>.Builder innerProjectContextsBuilder = ImmutableDictionary.CreateBuilder<string, IWorkspaceProjectContext>();
                string activeTargetFramework = string.Empty;
                IConfiguredProjectHostObject activeIntellisenseProjectHostObject = null;

                foreach (KeyValuePair<string, ConfiguredProject> kvp in configuredProjectsMap)
                {
                    string targetFramework = kvp.Key;
                    ConfiguredProject configuredProject = kvp.Value;
                    if (!TryGetConfiguredProjectState(configuredProject, out IWorkspaceProjectContext workspaceProjectContext, out IConfiguredProjectHostObject configuredProjectHostObject))
                    {
                        // Get the target path for the configured project.
                        ProjectProperties projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                        ConfigurationGeneral configurationGeneralProperties = await projectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                        targetPath = (string)await configurationGeneralProperties.TargetPath.GetValueAsync().ConfigureAwait(true);
                        string targetFrameworkMoniker = (string)await configurationGeneralProperties.TargetFrameworkMoniker.GetValueAsync().ConfigureAwait(true);
                        string displayName = GetDisplayName(configuredProject, projectData, targetFramework);
                        configuredProjectHostObject = _projectHostProvider.GetConfiguredProjectHostObject(_unconfiguredProjectHostObject, displayName, targetFrameworkMoniker);

                        // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
                        await _commonServices.ThreadingService.SwitchToUIThread();
                        workspaceProjectContext = _contextFactory.Value.CreateProjectContext(languageName, displayName, projectData.FullPath, projectGuid, configuredProjectHostObject, targetPath);

                        // By default, set "LastDesignTimeBuildSucceeded = false" to turn off diagnostics until first design time build succeeds for this project.
                        workspaceProjectContext.LastDesignTimeBuildSucceeded = false;

                        AddConfiguredProjectState(configuredProject, workspaceProjectContext, configuredProjectHostObject);
                    }

                    innerProjectContextsBuilder.Add(targetFramework, workspaceProjectContext);

                    if (activeIntellisenseProjectHostObject == null && configuredProject.ProjectConfiguration.Equals(activeProjectConfiguration))
                    {
                        activeIntellisenseProjectHostObject = configuredProjectHostObject;
                        activeTargetFramework = targetFramework;
                    }
                }

                _unconfiguredProjectHostObject.ActiveIntellisenseProjectHostObject = activeIntellisenseProjectHostObject;

                return new AggregateWorkspaceProjectContext(innerProjectContextsBuilder.ToImmutable(), configuredProjectsMap, activeTargetFramework, _unconfiguredProjectHostObject);
            });
        }

        private static string GetDisplayName(ConfiguredProject configuredProject, ProjectData projectData, string targetFramework)
        {
            // For cross targeting projects, we need to ensure that the display name is unique per every target framework.
            // This is needed for couple of reasons:
            //   (a) The display name is used in the editor project context combo box when opening source files that used by more than one inner projects.
            //   (b) Language service requires each active workspace project context in the current workspace to have a unique value for {ProjectFilePath, DisplayName}.
            return configuredProject.ProjectConfiguration.IsCrossTargeting() ?
                $"{projectData.DisplayName} ({targetFramework})" :
                projectData.DisplayName;
        }

        private struct ProjectData
        {
            public string FullPath;
            public string DisplayName;
        }
    }
}
