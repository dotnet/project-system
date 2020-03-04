// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    /// Suppresses the "Add Reference..." command in various menus.
    /// </summary>
    /// <remarks>
    /// All places that previously had this command should now feature <c>IDG_VS_CTXT_REFERENCEMANAGEMENT</c>
    /// which includes specific "Add ___ Reference..." commands, including "Add Assembly Reference...".
    /// </remarks>
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.ADDREFERENCE)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(ProjectSystem.Order.Default)]
    internal sealed class SuppressAddReferenceCommand : AbstractSingleNodeProjectCommand
    {
        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            return GetCommandStatusResult.Suppressed;
        }

        protected override Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            return TaskResult.False;
        }
    }
}
