// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    /// Hides the "Add Reference..." command in various menus.
    /// </summary>
    /// <remarks>
    /// All places that previously had this command should now feature <c>IDG_VS_CTXT_REFERENCEMANAGEMENT</c>
    /// which includes specific "Add ___ Reference..." commands, including "Add Assembly Reference...".
    /// </remarks>
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.ADDREFERENCE)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(ProjectSystem.Order.Default)]
    internal sealed class HideAddReferenceCommand : AbstractSingleNodeProjectCommand
    {
        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            progressiveStatus |= CommandStatus.Invisible;
            return GetCommandStatusResult.Handled(commandText, progressiveStatus);
        }

        protected override Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            return TaskResult.False;
        }
    }
}
