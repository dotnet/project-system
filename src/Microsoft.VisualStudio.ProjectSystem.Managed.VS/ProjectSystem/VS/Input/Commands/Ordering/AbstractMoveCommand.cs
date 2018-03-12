// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractMoveCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly ConfiguredProject _configuredProject;

        public AbstractMoveCommand(IPhysicalProjectTree projectTree, SVsServiceProvider serviceProvider, ConfiguredProject configuredProject)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(configuredProject, nameof(configuredProject));

            _projectTree = projectTree;
            _serviceProvider = serviceProvider;
            _configuredProject = configuredProject;
        }

        protected abstract bool CanMove(IProjectTree node);

        protected abstract bool TryMove(Project project, IProjectTree node);

        protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (!CanMove(node))
            {
                return await GetCommandStatusResult.Handled(commandText, CommandStatus.Ninched).ConfigureAwait(true);
            }

            return await GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled).ConfigureAwait(true);
        }

        private async Task<TResult> OpenProjectForWriteAsync<TResult>(ConfiguredProject project, Func<Project, TResult> action)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(project, nameof(action));

            var projectLockService = _configuredProject.Services.ProjectService.Services.ProjectLockService;

            using (ProjectWriteLockReleaser access = await projectLockService.WriteLockAsync())
            {
                await access.CheckoutAsync(project.UnconfiguredProject.FullPath)
                            .ConfigureAwait(true);

                Project evaluatedProject = await access.GetProjectAsync(project)
                                                       .ConfigureAwait(true);

                // Deliberately not async to reduce the type of
                // code you can run while holding the lock.
                return action(evaluatedProject);
            }
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            var didMove = await OpenProjectForWriteAsync(_configuredProject, project => TryMove(project, node)).ConfigureAwait(true);

            if (didMove)
            {
                // Wait for updating to finish before re-selecting the node that moved.
                // We need to re-select the node after it is moved in order to continuously move the node using hotkeys.
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(true);
                HACK_NodeHelper.Select(_configuredProject, _serviceProvider, node);
            }

            return didMove;
        }
    }
}
