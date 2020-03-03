// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    /// Suppresses the "Add Reference..." command in specific dependencies tree context menus.
    /// </summary>
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.ADDREFERENCE)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(ProjectSystem.Order.Default)]
    internal sealed class SuppressAddReferenceCommand : AbstractSingleNodeProjectCommand
    {
        // TODO once CPS inserts, use this field along with ContainsAny
//      private static readonly ProjectTreeFlags s_hideAddReferenceForFlags = DependencyTreeFlags.DependenciesRootNode + DependencyTreeFlags.TargetNode;

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
//          if (node.Flags.ContainsAny(s_hideAddReferenceForFlags))
            if (node.Flags.Contains(DependencyTreeFlags.DependenciesRootNode) || node.Flags.Contains(DependencyTreeFlags.TargetNode))
            {
                return GetCommandStatusResult.Suppressed;
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            return TaskResult.False;
        }
    }
}
