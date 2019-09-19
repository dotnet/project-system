// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
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
            if (node.Flags.Contains(DependencyTreeFlags.NuGetPackageDependency))
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
