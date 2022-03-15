// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal abstract class AbstractMoveCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IPhysicalProjectTree _projectTree;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectAccessor _accessor;

        protected AbstractMoveCommand(IPhysicalProjectTree projectTree, SVsServiceProvider serviceProvider, ConfiguredProject configuredProject, IProjectAccessor accessor)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(accessor, nameof(accessor));

            _projectTree = projectTree;
            _serviceProvider = serviceProvider;
            _configuredProject = configuredProject;
            _accessor = accessor;
        }

        protected abstract bool CanMove(IProjectTree node);

        protected abstract bool TryMove(Project project, IProjectTree node);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            return GetCommandStatusResult.Handled(
                commandText,
                CanMove(node) ? CommandStatus.Enabled : CommandStatus.Ninched);
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            bool didMove = false;

            await _accessor.OpenProjectForWriteAsync(_configuredProject, project => didMove = TryMove(project, node));

            if (didMove)
            {
                // Wait for updating to finish before re-selecting the node that moved.
                // We need to re-select the node after it is moved in order to continuously move the node using hotkeys.
                await _projectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true);
                await NodeHelper.SelectAsync(_configuredProject, _serviceProvider, node);
            }

            return didMove;
        }
    }
}
