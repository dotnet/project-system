// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Implementation of <see cref="IProjectContextProvider"/> that creates an <see cref="AggregateWorkspaceProjectContext"/>
    ///     based on the unique TargetFramework configurations of an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IProjectContextProvider))]
    internal partial class UnconfiguredProjectContextProvider : OnceInitializedOnceDisposed, IProjectContextProvider
    {
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IWorkspaceProjectContextFactory> _contextFactory;
        private readonly IProjectAsyncLoadDashboard _asyncLoadDashboard;
        private readonly ITaskScheduler _taskScheduler;
        private readonly List<AggregateWorkspaceProjectContext> _contexts = new List<AggregateWorkspaceProjectContext>();
        private readonly IProjectHostProvider _projectHostProvider;
        private readonly ActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;

        [ImportingConstructor]
        public UnconfiguredProjectContextProvider(IUnconfiguredProjectCommonServices commonServices,
                                                 Lazy<IWorkspaceProjectContextFactory> contextFactory,
                                                 IProjectAsyncLoadDashboard asyncLoadDashboard,
                                                 ITaskScheduler taskScheduler,
                                                 IProjectHostProvider projectHostProvider,
                                                 ActiveConfiguredProjectsProvider activeConfiguredProjectsProvider)
        {
            Requires.NotNull(commonServices, nameof(commonServices));
            Requires.NotNull(contextFactory, nameof(contextFactory));
            Requires.NotNull(asyncLoadDashboard, nameof(asyncLoadDashboard));
            Requires.NotNull(taskScheduler, nameof(taskScheduler));
            Requires.NotNull(projectHostProvider, nameof(projectHostProvider));
            Requires.NotNull(activeConfiguredProjectsProvider, nameof(activeConfiguredProjectsProvider));

            _commonServices = commonServices;
            _contextFactory = contextFactory;
            _asyncLoadDashboard = asyncLoadDashboard;
            _taskScheduler = taskScheduler;
            _projectHostProvider = projectHostProvider;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
        }

        public async Task<AggregateWorkspaceProjectContext> CreateProjectContextAsync()
        {
            EnsureInitialized();

            var context = await CreateProjectContextAsyncCore().ConfigureAwait(false);
            if (context == null)
                return null;

            lock (_contexts)
            {
                // There's a race here, by the time we've created the project context,
                // the project could have been renamed, handle this.
                var projectData = GetProjectData();

                context.SetProjectFilePathAndDisplayName(projectData.FullPath, projectData.DisplayName);
                _contexts.Add(context);
            }

            return context;
        }

        public async Task ReleaseProjectContextAsync(AggregateWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            lock (_contexts)
            {
                if (!_contexts.Remove(context))
                    throw new ArgumentException("Specified context was not created by this instance, or has already been unregistered.");
            }

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            await _commonServices.ThreadingService.SwitchToUIThread();

            context.Dispose();
        }

        protected override void Initialize()
        {
            _commonServices.Project.ProjectRenamed += OnProjectRenamed;
        }

        protected override void Dispose(bool disposing)
        {
            _commonServices.Project.ProjectRenamed -= OnProjectRenamed;
        }

        private Task OnProjectRenamed(object sender, ProjectRenamedEventArgs args)
        {
            lock (_contexts)
            {
                var projectData = GetProjectData();

                foreach (var context in _contexts)
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

        private async Task<Guid> GetProjectGuidAsync()
        {
            ConfigurationGeneral properties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                                     .ConfigureAwait(false);
            Guid guid;
            Guid.TryParse((string)await properties.ProjectGuid.GetValueAsync().ConfigureAwait(false), out guid);

            return guid;
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

        private async Task<AggregateWorkspaceProjectContext> CreateProjectContextAsyncCore()
        {
            string languageName = await GetLanguageServiceName().ConfigureAwait(false);
            if (string.IsNullOrEmpty(languageName))
                return null;
            
            Guid projectGuid = await GetProjectGuidAsync().ConfigureAwait(false);
            string targetPath = await GetTargetPathAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(targetPath))
                return null;

            // Don't initialize until the project has been loaded into the IDE and available in Solution Explorer
            await _asyncLoadDashboard.ProjectLoadedInHost.ConfigureAwait(false);

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            return await _taskScheduler.RunAsync(TaskSchedulerPriority.UIThreadBackgroundPriority, async () => 
            {
                await _commonServices.ThreadingService.SwitchToUIThread();

                var projectData = GetProjectData();
                
                // Get the set of active configured projects ignoring target framework.
                var configuredProjectsMap = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync().ConfigureAwait(true);

                // Get the unconfigured project host object (shared host object).
                IUnconfiguredProjectHostObject unconfiguredProjectHostObject = _projectHostProvider.GetUnconfiguredProjectHostObject(_commonServices.Project);
                var activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;

                var innerProjectContextsBuilder = ImmutableDictionary.CreateBuilder<string, IWorkspaceProjectContext>();
                IConfiguredProjectHostObject activeIntellisenseProjectHostObject = null;
                foreach (var kvp in configuredProjectsMap)
                {
                    var targetFramework = kvp.Key;
                    var configuredProject = kvp.Value;

                    // Get the target path for the configured project.
                    var projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                    var configurationGeneralProperties = await projectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    targetPath = (string)await configurationGeneralProperties.TargetPath.GetValueAsync().ConfigureAwait(true);
                    targetPath = NormalizeTargetPath(targetPath, projectData);
                    var displayName = GetDisplayName(configuredProject, projectData, targetFramework);
                    IConfiguredProjectHostObject configuredProjectHostObject = _projectHostProvider.GetConfiguredProjectHostObject(unconfiguredProjectHostObject, displayName);

                    // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
                    await _commonServices.ThreadingService.SwitchToUIThread();
                    var workspaceProjectContext = _contextFactory.Value.CreateProjectContext(languageName, displayName, projectData.FullPath, projectGuid, configuredProjectHostObject, targetPath);
                    innerProjectContextsBuilder.Add(targetFramework, workspaceProjectContext);

                    if (activeIntellisenseProjectHostObject == null && configuredProject.ProjectConfiguration.Equals(activeProjectConfiguration))
                    {
                        activeIntellisenseProjectHostObject = configuredProjectHostObject;
                    }
                }

                unconfiguredProjectHostObject.ActiveIntellisenseProjectHostObject = activeIntellisenseProjectHostObject;

                var activeTargetFramework = activeProjectConfiguration.Dimensions[TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName];
                return new AggregateWorkspaceProjectContext(innerProjectContextsBuilder.ToImmutable(), activeTargetFramework, unconfiguredProjectHostObject);
            });
        }

        private static string NormalizeTargetPath(string targetPath, ProjectData projectData)
        {
            Requires.NotNullOrEmpty(targetPath, nameof(targetPath));

            if (!Path.IsPathRooted(targetPath))
            {
                var directory = Path.GetDirectoryName(projectData.FullPath);
                targetPath = Path.Combine(directory, targetPath);
            }

            return targetPath;
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
