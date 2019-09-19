// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.QUICKOBJECTSEARCH)]
    [AppliesTo(ProjectCapability.PackageReferences)]
    [Order(ProjectSystem.Order.Default)]
    internal sealed class SuppressObjectBrowserForPackageReferenceCommand : AbstractSingleNodeProjectCommand
    {
        private static readonly Task<CommandStatusResult> s_suppressedResult = Task.FromResult(new CommandStatusResult(handled: true, null, CommandStatus.NotSupported | CommandStatus.Invisible));
        
        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (node.Flags.Contains(DependencyTreeFlags.NuGetPackageDependency))
            {
                // Suppress "Open in Object Browser" for NuGet packages
                return s_suppressedResult;
            }
         
            return CommandStatusResult.Unhandled.AsTask();
        }

        protected override Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            return TaskResult.False;
        }
    }
}
