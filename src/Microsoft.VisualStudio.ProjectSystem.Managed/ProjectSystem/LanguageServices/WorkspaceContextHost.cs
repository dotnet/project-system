// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Responsible for creating and initializing <see cref="IWorkspaceProjectContext"/> and sending 
    ///     on changes to the project to the <see cref="IApplyChangesToWorkspaceContext"/> service.
    /// </summary>
    [Export(typeof(IImplicitlyActiveService))]
    [AppliesTo(ProjectCapability.DotNetLanguageService2)]
    internal partial class WorkspaceContextHost : AbstractMultiLifetimeComponent, IImplicitlyActiveService
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly IWorkspaceProjectContextProvider _workspaceProjectContextProvider;
        private readonly IActiveWorkspaceProjectContextTracker _activeWorkspaceProjectContextTracker;
        private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

        [ImportingConstructor]
        public WorkspaceContextHost(ConfiguredProject project,
                                    IProjectThreadingService threadingService,
                                    IUnconfiguredProjectTasksService tasksService,
                                    IProjectSubscriptionService projectSubscriptionService,
                                    IWorkspaceProjectContextProvider workspaceProjectContextProvider,
                                    IActiveWorkspaceProjectContextTracker activeWorkspaceProjectContextTracker,
                                    ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _threadingService = threadingService;
            _tasksService = tasksService;
            _projectSubscriptionService = projectSubscriptionService;
            _workspaceProjectContextProvider = workspaceProjectContextProvider;
            _activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker;
            _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
        }

        public Task ActivateAsync()
        {
            return LoadAsync();
        }

        public Task DeactivateAsync()
        {
            return UnloadAsync();
        }

        protected override IMultiLifetimeInstance CreateInstance()
        {
            return new WorkspaceContextHostInstance(_project, _threadingService, _tasksService, _projectSubscriptionService, _workspaceProjectContextProvider, _activeWorkspaceProjectContextTracker, _applyChangesToWorkspaceContextFactory);
        }
    }
}
