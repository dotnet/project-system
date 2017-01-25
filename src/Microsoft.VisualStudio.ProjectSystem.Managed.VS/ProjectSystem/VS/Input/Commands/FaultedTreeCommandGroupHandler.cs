using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal class FaultedTreeCommandGroupHandler : IAsyncCommandGroupHandler
    {
        [ExportCommandGroup(CommandGroup.VisualStudioStandard97)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(int.MaxValue)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard97Handler =>
            new FaultedTreeCommandGroupHandler(new List<long> { VisualStudioStandard97CommandId.UnloadProject });

        [ExportCommandGroup(CommandGroup.VisualStudioStandard2k)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(int.MaxValue)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard2kHandler =>
            new FaultedTreeCommandGroupHandler(new List<long> { VisualStudioStandard2kCommandId.EditProjectFile });

        [ExportCommandGroup(CommandGroup.VisualStudioStandard2010)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(int.MaxValue)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard2010Handler => new FaultedTreeCommandGroupHandler(null);

        [ExportCommandGroup(CommandGroup.VisualStudioStandard11)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(int.MaxValue)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard11Handler => new FaultedTreeCommandGroupHandler(null);

        [ExportCommandGroup(CommandGroup.VisualStudioStandard12)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(int.MaxValue)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard12Handler => new FaultedTreeCommandGroupHandler(null);

        [ExportCommandGroup(CommandGroup.VisualStudioStandard14)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(int.MaxValue)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard14Handler => new FaultedTreeCommandGroupHandler(null);

        [ExportCommandGroup(CommandGroup.VisualStudioStandard15)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(100)] // We want to be ahead of the standard handlers
        public static IAsyncCommandGroupHandler VisualStudioStandard15Handler => new FaultedTreeCommandGroupHandler(null);

        private readonly IList<long> _allowedCommands;

        private FaultedTreeCommandGroupHandler(IList<long> allowedCommands)
        {
            _allowedCommands = allowedCommands ?? new List<long>();
        }

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            // Only handle if we've selected 1 node, and the tree is faulted
            if (nodes.Count != 1 || !nodes.First().Flags.Contains("FaultTree"))
            {
                return GetCommandStatusResult.Unhandled;
            }

            // If the command is in the list of commands that we want to allow to show, return unhandled so the real handler can get it.
            // Otherwise, return not supported
            if (_allowedCommands.Contains(commandId))
            {
                return GetCommandStatusResult.Unhandled;
            }
            else
            {
                return GetCommandStatusResult.Handled(string.Empty, CommandStatus.Invisible);
            }
        }

        public Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            // We don't handle any tasks.
            return Task.FromResult(false);
        }
    }
}
