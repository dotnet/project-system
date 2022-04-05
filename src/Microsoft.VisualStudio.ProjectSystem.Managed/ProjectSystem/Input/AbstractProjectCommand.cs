// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Input
{
    /// <summary>
    ///     Provides the base <see langword="abstract"/> class for all commands that operate on <see cref="IProjectTree"/> nodes.
    /// </summary>
    internal abstract class AbstractProjectCommand : IAsyncCommandGroupHandler
    {
        private readonly Lazy<long[]> _commandIds;

        protected AbstractProjectCommand()
        {
            _commandIds = new Lazy<long[]>(() => GetCommandIds(this));
        }

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            Requires.NotNull(nodes, nameof(nodes));

            foreach (long otherCommandId in _commandIds.Value)
            {
                if (otherCommandId == commandId)
                    return GetCommandStatusAsync(nodes, focused, commandText, progressiveStatus);
            }

            return GetCommandStatusResult.Unhandled;
        }

        public Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            Requires.NotNull(nodes, nameof(nodes));

            foreach (long otherCommandId in _commandIds.Value)
            {
                if (otherCommandId == commandId)
                    return TryHandleCommandAsync(nodes, focused, commandExecuteOptions, variantArgIn, variantArgOut);
            }

            return TaskResult.False;
        }

        protected abstract Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, bool focused, string? commandText, CommandStatus progressiveStatus);

        protected abstract Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut);

        private static long[] GetCommandIds(AbstractProjectCommand command)
        {
            var attribute = (ProjectCommandAttribute?)Attribute.GetCustomAttribute(command.GetType(), typeof(ProjectCommandAttribute));

            // All ProjectCommand's should be marked with [ProjectCommandAttribute]
            Assumes.NotNull(attribute);

            return attribute.CommandIds;
        }
    }
}
