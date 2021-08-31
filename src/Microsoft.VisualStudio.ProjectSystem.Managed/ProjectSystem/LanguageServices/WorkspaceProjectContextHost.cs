// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Responsible for creating and initializing <see cref="IWorkspaceProjectContext"/> and sending
    ///     on changes to the project to the <see cref="IApplyChangesToWorkspaceContext"/> service.
    /// </summary>
    [Export(typeof(IImplicitlyActiveConfigurationComponent))]
    [Export(typeof(IWorkspaceProjectContextHost))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal partial class WorkspaceProjectContextHost : AbstractMultiLifetimeComponent<WorkspaceProjectContextHost.WorkspaceProjectContextHostInstance>, IImplicitlyActiveConfigurationComponent, IWorkspaceProjectContextHost
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly IWorkspaceProjectContextProvider _workspaceProjectContextProvider;
        private readonly IActiveEditorContextTracker _activeWorkspaceProjectContextTracker;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
        private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;
        private readonly IDataProgressTrackerService _dataProgressTrackerService;
        private readonly IProjectBuildSnapshotService _projectBuildSnapshotService;

        [ImportingConstructor]
        public WorkspaceProjectContextHost(ConfiguredProject project,
                                           IProjectThreadingService threadingService,
                                           IUnconfiguredProjectTasksService tasksService,
                                           IProjectSubscriptionService projectSubscriptionService,
                                           IWorkspaceProjectContextProvider workspaceProjectContextProvider,
                                           IActiveEditorContextTracker activeWorkspaceProjectContextTracker,
                                           IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
                                           ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory,
                                           IDataProgressTrackerService dataProgressTrackerService,
                                           IProjectBuildSnapshotService projectBuildSnapshotService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _threadingService = threadingService;
            _tasksService = tasksService;
            _projectSubscriptionService = projectSubscriptionService;
            _workspaceProjectContextProvider = workspaceProjectContextProvider;
            _activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
            _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
            _dataProgressTrackerService = dataProgressTrackerService;
            _projectBuildSnapshotService = projectBuildSnapshotService;
        }

        public Task ActivateAsync()
        {
            return LoadAsync();
        }

        public Task DeactivateAsync()
        {
            return UnloadAsync();
        }

        public Task PublishAsync(CancellationToken cancellationToken = default)
        {
            return WaitForLoadedAsync(cancellationToken);
        }

        public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
        {
            Requires.NotNull(action, nameof(action));

            WorkspaceProjectContextHostInstance instance = await WaitForLoadedAsync();

            // Throws ActiveProjectConfigurationChangedException if 'instance' is Disposed
            await instance.OpenContextForWriteAsync(action);
        }

        public async Task<T?> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
        {
            Requires.NotNull(action, nameof(action));

            WorkspaceProjectContextHostInstance instance = await WaitForLoadedAsync();

            // Throws ActiveProjectConfigurationChangedException if 'instance' is Disposed
            return await instance.OpenContextForWriteAsync(action);
        }

        protected override WorkspaceProjectContextHostInstance CreateInstance()
        {
            return new WorkspaceProjectContextHostInstance(
                _project,
                _threadingService,
                _tasksService,
                _projectSubscriptionService,
                _workspaceProjectContextProvider,
                _activeWorkspaceProjectContextTracker,
                _activeConfiguredProjectProvider,
                _applyChangesToWorkspaceContextFactory,
                _dataProgressTrackerService,
                _projectBuildSnapshotService);
        }
    }
}
