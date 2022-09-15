// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    ///     Provides the <see langword="abstract"/> base class for commands
    ///     that handle Explorer-like commands for dependency <see cref="IProjectTree"/>
    ///     nodes.
    /// </summary>
    internal abstract class AbstractDependencyExplorerCommand : AbstractProjectCommand
    {
        private readonly UnconfiguredProject _project;

        protected AbstractDependencyExplorerCommand(UnconfiguredProject project)
        {
            _project = project;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            // Only handle when Solution Explorer has focus so that we don't take over Tab Well handling
            if (focused && nodes.All(CanOpen))
            {
                return GetCommandStatusResult.Handled(commandText, progressiveStatus | CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            // Only handle when Solution Explorer has focus so that we don't take over Tab Well handling
            if (focused && nodes.All(CanOpen))
            {
                foreach (IProjectTree node in nodes)
                {
                    string? path = await DependencyServices.GetBrowsePathAsync(_project, node);
                    if (path is null)
                        continue;

                    Open(path);
                }

                return true;
            }

            return false;
        }

        protected abstract bool CanOpen(IProjectTree node);

        protected abstract void Open(string path);
    }
}
