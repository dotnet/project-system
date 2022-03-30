// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Input
{
    /// <summary>
    ///     Provides the base <see langword="abstract"/> class for all commands that handle a single <see cref="IProjectTree"/> node.
    /// </summary>
    internal abstract class AbstractSingleNodeProjectCommand : AbstractProjectCommand
    {
        protected sealed override Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (nodes.Count == 1)
            {
                return GetCommandStatusAsync(nodes.First(), focused, commandText, progressiveStatus);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected sealed override Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (nodes.Count == 1)
            {
                return TryHandleCommandAsync(nodes.First(), focused, commandExecuteOptions, variantArgIn, variantArgOut);
            }

            return TaskResult.False;
        }

        protected abstract Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus);

        protected abstract Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut);
    }
}
