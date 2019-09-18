// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Ensures that the <see cref="IWorkspaceProjectContext"/> for the "active" configuration has 
    ///     been loaded by the time users and extensions can interact with the project.
    /// </summary>
    /// <remarks>
    ///     It is important to make sure Roslyn is aware of the project by the time the project can be 
    ///     interacted with so that restored documents and other features used quickly after solution 
    ///     load behave correctly and have "project context".
    /// </remarks>
    internal class WorkspaceProjectContextHostInitiator
    {
        private readonly UnconfiguredProject _project;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IProjectFaultHandlerService _projectFaultHandler;

        [ImportingConstructor]
        public WorkspaceProjectContextHostInitiator(UnconfiguredProject project, IUnconfiguredProjectTasksService tasksService, IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost, IProjectFaultHandlerService projectFaultHandler)
        {
            _project = project;
            _tasksService = tasksService;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
            _projectFaultHandler = projectFaultHandler;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.AfterLoadInitialConfiguration, completeBy: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public Task InitializeAsync()
        {
            // While we want make sure it's loaded before PrioritizedProjectLoadedInHost, 
            // we don't want to block project factory completion on its load, so fire and forget
            Task result = _tasksService.PrioritizedProjectLoadedInHostAsync(() => _activeWorkspaceProjectContextHost.PublishAsync());

            _projectFaultHandler.Forget(result, _project, ProjectFaultSeverity.LimitedFunctionality);

            return Task.CompletedTask;
        }
    }
}
