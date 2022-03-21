// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    /// Suppresses the "Open in Object Browser" command for NuGet packages.
    /// </summary>
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.QUICKOBJECTSEARCH)]
    [AppliesTo(ProjectCapability.PackageReferences)]
    [Order(ProjectSystem.Order.Default)]
    internal sealed class SuppressObjectBrowserForPackageReferenceCommand : AbstractSingleNodeProjectCommand
    {
        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (node.Flags.Contains(DependencyTreeFlags.PackageDependency))
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
