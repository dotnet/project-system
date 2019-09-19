// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Command handler that provides special handling for standard commands on dependency nodes.
    /// </summary>
    /// <remarks>
    /// CPS provides a handler for the same commands, however it does not know anything special about dependency nodes.
    /// </remarks>
    [ExportCommandGroup(VSConstants.CMDSETID.StandardCommandSet2K_string)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(10)]
    internal sealed class DependenciesNodeCommandGroupHandler : ICommandGroupHandler
    {
        private static readonly CommandStatusResult s_suppressedResult = new CommandStatusResult(handled: true, null, CommandStatus.NotSupported | CommandStatus.Invisible);

        public CommandStatusResult GetCommandStatus(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            var cmdId = (VSConstants.VSStd2KCmdID)commandId;

            switch (cmdId)
            {
                case VSConstants.VSStd2KCmdID.QUICKOBJECTSEARCH: // Open in Object Browser
                {
                    if (nodes.Count == 1)
                    {
                        IProjectTree tree = nodes.First();

                        if (tree.Flags.Contains(DependencyTreeFlags.NuGetPackageDependency))
                        {
                            // Suppress "Open in Object Browser" for NuGet packages
                            return s_suppressedResult;
                        }
                    }

                    break;
                }
            }

            return CommandStatusResult.Unhandled;
        }

        public bool TryHandleCommand(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            return false;
        }
    }
}
