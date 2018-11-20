// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Implementation of <see cref="IProjectContextProvider"/> that creates an <see cref="AggregateWorkspaceProjectContext"/>
    ///     based on the unique TargetFramework configurations of an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IProjectContextProvider))]
    internal partial class UnconfiguredProjectContextProvider : EnsureOnceInitializedOnceDisposedAsync, IProjectContextProvider
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IWorkspaceProjectContextFactory> _contextFactory;
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
                                                 IProjectHostProvider projectHostProvider,
                                                 IActiveConfiguredProjectsProvider activeConfiguredProjectsProvider,
                                                 ISafeProjectGuidService projectGuidService)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _commonServices = commonServices;
            _contextFactory = contextFactory;
            _projectHostProvider = projectHostProvider;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
            _projectGuidService = projectGuidService;
            _unconfiguredProjectHostObject = _projectHostProvider.UnconfiguredProjectHostObject;
        }

        public async Task<AggregateWorkspaceProjectContext> CreateProjectContextAsync()
        {
            await InitializeAsync();

            AggregateWorkspaceProjectContext context = await CreateProjectContextAsyncCore();
            if (context == null)
                return null;

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

        public async Task ReleaseProjectContextAsync(AggregateWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            ImmutableHashSet<IWorkspaceProjectContext> usedProjectContexts = null;
            await ExecuteWithinLockAsync(() =>
            {
                if (!_contexts.Remove(context))
                    throw new ArgumentException("Specified context was not created by this instance, or has already been unregistered.");

                // Update the maps storing configured project host objects and project contexts which are shared across created contexts.
                // We can remove the ones which are only used by the current context being released.
                RemoveUnusedConfiguredProjectsState_NoLock();

                usedProjectContexts = _configuredProjectContextsMap.Values.ToImmutableHashSet();
            });

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

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _commonServices.Project.ProjectRenamed += OnProjectRenamed;
            return Task.CompletedTask;
        }
        protected override Task DisposeCoreAsync(bool initialized)
        {
            _commonServices.Project.ProjectRenamed -= OnProjectRenamed;
            _unconfiguredProjectHostObject.Dispose();
            _gate.Dispose();
            return Task.CompletedTask;
        }

        private Task OnProjectRenamed(object sender, ProjectRenamedEventArgs args)
        {
            return ExecuteWithinLockAsync(() =>
            {
                ProjectData projectData = GetProjectData();

                foreach (AggregateWorkspaceProjectContext context in _contexts)
                {
                    context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                }
            });
        }

        // Returns the name that is the handshake between Roslyn and the csproj/vbproj
        private async Task<string> GetLanguageServiceName()
        {
            ConfigurationGeneral properties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();

            return (string)await properties.LanguageServiceName.GetValueAsync();
        }

        private async Task<string> GetTargetPathAsync()
        {
            ConfigurationGeneral properties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();
            return (string)await properties.TargetPath.GetValueAsync();
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
            using (_gate.DisposableWait())
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
            using (_gate.DisposableWait())
            {
                _configuredProjectContextsMap.Add(configuredProject, workspaceProjectContext);
                _configuredProjectHostObjectsMap.Add(configuredProject, configuredProjectHostObject);
            }
        }

        private async Task<AggregateWorkspaceProjectContext> CreateProjectContextAsyncCore()
        {
            string languageName = await GetLanguageServiceName();
            if (string.IsNullOrEmpty(languageName))
                return null;

            Guid projectGuid = await _projectGuidService.GetProjectGuidAsync();

            string targetPath = await GetTargetPathAsync();
            if (string.IsNullOrEmpty(targetPath))
                return null;


            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            await _commonServices.ThreadingService.SwitchToUIThread();

            ProjectData projectData = GetProjectData();

            // Get the set of active configured projects ignoring target framework.
#pragma warning disable CS0618 // Type or member is obsolete
            ImmutableDictionary<string, ConfiguredProject> configuredProjectsMap = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync();
#pragma warning restore CS0618 // Type or member is obsolete

            // Get the unconfigured project host object (shared host object).
            var configuredProjectsToRemove = new HashSet<ConfiguredProject>(_configuredProjectHostObjectsMap.Keys);
            ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;

            ImmutableDictionary<string, IWorkspaceProjectContext>.Builder innerProjectContextsBuilder = ImmutableDictionary.CreateBuilder<string, IWorkspaceProjectContext>();
            string activeTargetFramework = string.Empty;
            IConfiguredProjectHostObject activeIntellisenseProjectHostObject = null;

            foreach ((string targetFramework, ConfiguredProject configuredProject) in configuredProjectsMap)
            {
                if (!TryGetConfiguredProjectState(configuredProject, out IWorkspaceProjectContext workspaceProjectContext, out IConfiguredProjectHostObject configuredProjectHostObject))
                {
                    // Get the target path for the configured project.
                    ProjectProperties projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                    ConfigurationGeneral configurationGeneralProperties = await projectProperties.GetConfigurationGeneralPropertiesAsync();
                    targetPath = (string)await configurationGeneralProperties.TargetPath.GetValueAsync();
                    string targetFrameworkMoniker = (string)await configurationGeneralProperties.TargetFrameworkMoniker.GetValueAsync();
                    string workspaceProjectContextId = GetWorkspaceContextId(configuredProject);
                    configuredProjectHostObject = _projectHostProvider.GetConfiguredProjectHostObject(_unconfiguredProjectHostObject, workspaceProjectContextId, targetFrameworkMoniker);

                    // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
                    await _commonServices.ThreadingService.SwitchToUIThread();

                    // NOTE: Despite CreateProjectContext taking a "displayName"; it's actually sets both "WorkspaceProjectContextId", "DisplayName", and default "AssemblyName".
                    // Unlike the latter properties, we cannot change WorkspaceProjectContextId once set, so we pass it as the display name.
                    workspaceProjectContext = _contextFactory.Value.CreateProjectContext(languageName, workspaceProjectContextId, projectData.FullPath, projectGuid, configuredProjectHostObject, targetPath);
                    workspaceProjectContext.DisplayName = GetDisplayName(configuredProject, projectData, targetFramework);

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
        }

        private static string GetWorkspaceContextId(ConfiguredProject configuredProject)
        {
            // WorkspaceContextId must be unique across the entire solution, therefore as we fire up a workspace context 
            // per implicitly active config, we factor in both the full path of the project + the name of the config.
            //
            // NOTE: Roslyn also uses this name as the default "AssemblyName" until we explicitly set it, so we need to make 
            // sure it doesn't contain any invalid path characters.
            //
            // For example:
            //      C:\Project\Project.csproj (Debug|AnyCPU)
            //      C:\Project\MultiTarget.csproj (Debug|AnyCPU|net45)

            return $"{configuredProject.UnconfiguredProject.FullPath} ({configuredProject.ProjectConfiguration.Name.Replace("|", "_")})";
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

        private Task ExecuteWithinLockAsync(Action action) => _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, action);

        private struct ProjectData
        {
            public string FullPath;
            public string DisplayName;
        }
    }
}
