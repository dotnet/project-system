// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    ///     Opens the folder for references that are backed by folders on disk, including
    ///     package references, framework references and SDK references.
    /// </summary>
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.ExploreFolderInWindows)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order.Default)]
    internal class ExploreDependencyFolderInWindowsCommand : AbstractDependencyExplorerCommand
    {
        [ImportingConstructor]
        public ExploreDependencyFolderInWindowsCommand(UnconfiguredProject project)
            : base(project)
        {
        }

        protected override bool CanOpen(IProjectTree node)
        {
            return node.Flags.Contains(DependencyTreeFlags.GenericDependency | DependencyTreeFlags.SupportsFolderBrowse);
        }

        protected override void Open(string path)
        {
            // Tell Explorer just open the contents of the folder, selecting nothing
            ShellExecute("explore", path);
        }
    }
}
