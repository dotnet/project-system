// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Threading;

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
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;

        [ImportingConstructor]
        public WorkspaceProjectContextHostInitiator(IUnconfiguredProjectTasksService tasksService, IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost)
        {
            _tasksService = tasksService;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
        }

#pragma warning disable RS0030 // Do not used banned APIs
        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.AfterLoadInitialConfiguration, completeBy: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
#pragma warning restore RS0030
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public Task InitializeAsync()
        {
            // While we want make sure it's loaded before PrioritizedProjectLoadedInHost, 
            // we don't want to block project factory completion on its load, so fire and forget
            _tasksService.PrioritizedProjectLoadedInHostAsync(() => _activeWorkspaceProjectContextHost.PublishAsync())
                         .Forget();

            return Task.CompletedTask;
        }
    }
}
