// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    ///     Opens the containing folder for references.
    /// </summary>
    /// <remarks>
    ///     NOTE: Similar to folders and the project node, this command is supported 
    ///     for container-like references, such as Package References, however, is not 
    ///     placed on the context menu by default, in lieu of 
    ///     VSConstants.VSStd2KCmdID.ExploreFolderInWindows.
    /// </remarks>
    [ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.BrowseToFileInExplorer)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order.Default)]
    internal class BrowseToDependencyInExplorerCommand : AbstractDependencyExplorerCommand
    {
        private readonly IFileExplorer _fileExplorer;

        [ImportingConstructor]
        public BrowseToDependencyInExplorerCommand(UnconfiguredProject project, IFileExplorer fileExplorer)
            : base(project)
        {
            _fileExplorer = fileExplorer;
        }

        protected override bool CanOpen(IProjectTree node)
        {
            return node.Flags.Contains(DependencyTreeFlags.Dependency | DependencyTreeFlags.SupportsBrowse);
        }

        protected override void Open(string path)
        {
            _fileExplorer.OpenContainingFolder(path);
        }
    }
}
