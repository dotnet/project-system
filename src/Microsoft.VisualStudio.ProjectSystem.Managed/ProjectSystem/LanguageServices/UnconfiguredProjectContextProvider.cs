// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Implementation of <see cref="IProjectContextProvider"/> that creates an <see cref="IWorkspaceProjectContext"/>
    ///     based on the an <see cref="UnconfiguredProject"/>.
    /// </summary>
    [Export(typeof(IProjectContextProvider))]
    internal partial class UnconfiguredProjectContextProvider : OnceInitializedOnceDisposed, IProjectContextProvider
    {
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IWorkspaceProjectContextFactory> _contextFactory;
        private readonly IProjectAsyncLoadDashboard _asyncLoadDashboard;
        private readonly ITaskScheduler _taskScheduler;
        private readonly List<IWorkspaceProjectContext> _contexts = new List<IWorkspaceProjectContext>();

        [ImportingConstructor]
        public UnconfiguredProjectContextProvider(IUnconfiguredProjectCommonServices commonServices,
                                                 Lazy<IWorkspaceProjectContextFactory> contextFactory,
                                                 IProjectAsyncLoadDashboard asyncLoadDashboard,
                                                 ITaskScheduler taskScheduler)
        {
            Requires.NotNull(commonServices, nameof(commonServices));
            Requires.NotNull(contextFactory, nameof(contextFactory));
            Requires.NotNull(asyncLoadDashboard, nameof(asyncLoadDashboard));
            Requires.NotNull(taskScheduler, nameof(taskScheduler));

            _commonServices = commonServices;
            _contextFactory = contextFactory;
            _asyncLoadDashboard = asyncLoadDashboard;
            _taskScheduler = taskScheduler;
        }

        public async Task<IWorkspaceProjectContext> CreateProjectContextAsync()
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

                context.ProjectFilePath = projectData.FullPath;
                context.DisplayName = projectData.DisplayName;

                _contexts.Add(context);
            }

            return context;
        }

        public async Task ReleaseProjectContextAsync(IWorkspaceProjectContext context)
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
                    context.ProjectFilePath = projectData.FullPath;
                    context.DisplayName = projectData.DisplayName;
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

        private async Task<IWorkspaceProjectContext> CreateProjectContextAsyncCore()
        {
            string languageName = await GetLanguageServiceName().ConfigureAwait(false);
            if (string.IsNullOrEmpty(languageName))
                return null;
            
            Guid projectGuid = await GetProjectGuidAsync().ConfigureAwait(false);
            string targetPath = await GetTargetPathAsync().ConfigureAwait(false);

            // Don't initialize until the project has been loaded into the IDE and available in Solution Explorer
            await _asyncLoadDashboard.ProjectLoadedInHost.ConfigureAwait(false);

            // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
            return await _taskScheduler.RunAsync(TaskSchedulerPriority.UIThreadBackgroundPriority, async () => 
            {
                await _commonServices.ThreadingService.SwitchToUIThread();

                var projectData = GetProjectData();

                return _contextFactory.Value.CreateProjectContext(languageName, projectData.DisplayName, projectData.FullPath, projectGuid, _commonServices.Project.Services.HostObject, targetPath);
            });
        }

        private struct ProjectData
        {
            public string FullPath;
            public string DisplayName;
        }
    }
}
