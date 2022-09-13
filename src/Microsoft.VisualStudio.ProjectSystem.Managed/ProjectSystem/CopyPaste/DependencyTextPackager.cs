// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.CopyPaste
{
    /// <summary>
    ///     Packages dependency <see cref="IProjectTree"/> nodes as text for the "Copy Full Path" command.
    /// </summary>
    [Export(typeof(ICopyPackager))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order.Default)]
    internal class DependencyTextPackager : ICopyPackager
    {
        private static readonly ImmutableHashSet<int> s_formats = ImmutableHashSet.Create<int>(ClipboardFormat.CF_TEXT, ClipboardFormat.CF_UNICODETEXT);
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public DependencyTextPackager(UnconfiguredProject project)
        {
            _project = project;
        }

        public IImmutableSet<int> ClipboardDataFormats
        {
            get { return s_formats; }
        }

        public CopyPasteOperations GetAllowedOperations(IEnumerable<IProjectTree> selectedNodes, IProjectTreeProvider currentProvider)
        {
            return IsValidSetOfNodes(selectedNodes) ? CopyPasteOperations.Copy : CopyPasteOperations.None;
        }

        public async Task<IEnumerable<Tuple<int, IntPtr>>> GetPointerToDataAsync(IReadOnlyCollection<int> types, IEnumerable<IProjectTree> selectedNodes, IProjectTreeProvider currentProvider)
        {
            var paths = new StringBuilder();
            var data = new List<Tuple<int, IntPtr>>();

            foreach (IProjectTree node in selectedNodes)
            {
                string? path = await DependencyServices.GetBrowsePathAsync(_project, node);
                if (path is null)
                    continue;

                // Note we leave trailing slashes to mimic what happens with normal folders
                if (node.Flags.Contains(DependencyTreeFlags.SupportsFolderBrowse))
                    path = PathHelper.EnsureTrailingSlash(path);

                if (paths.Length > 0)
                {
                    paths.AppendLine();
                }

                paths.Append(path); 
            }

            if (types.Contains(ClipboardFormat.CF_TEXT))
            {
                data.Add(new Tuple<int, IntPtr>(ClipboardFormat.CF_TEXT, Marshal.StringToHGlobalAnsi(paths.ToString())));
            }

            if (types.Contains(ClipboardFormat.CF_UNICODETEXT))
            {
                data.Add(new Tuple<int, IntPtr>(ClipboardFormat.CF_UNICODETEXT, Marshal.StringToHGlobalUni(paths.ToString())));
            }

            return data;
        }

        private static bool IsValidSetOfNodes(IEnumerable<IProjectTree> treeNodes)
        {
            Requires.NotNull(treeNodes, nameof(treeNodes));

            return treeNodes.All(node => node.Flags.Contains(DependencyTreeFlags.Dependency | DependencyTreeFlags.SupportsBrowse));
        }
    }
}
