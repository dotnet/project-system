using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    internal abstract class AbstractFindByItemTypeSpecialFileProvider : AbstractSpecialFileProvider
    {
        protected abstract string ItemType { get; }

        [ImportingConstructor]
        protected AbstractFindByItemTypeSpecialFileProvider(
            IPhysicalProjectTree projectTree,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
            IFileSystem fileSystem)
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem)
        {
        }

        protected override Task<IProjectTree> FindFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default) => FindFileAsync(ProjectTree.CurrentTree);

        protected override Task<string> CreateFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default) => CreateFileAsync(ProjectTree.CurrentTree);

        private async Task<IProjectTree> FindFileAsync(IProjectTree tree)
        {
            foreach (IProjectTree node in tree.Children)
            {
                if (node.Flags.Contains(ProjectTreeFlags.FileOnDisk)
                    && string.Equals(node.BrowseObjectProperties.ItemType, ItemType, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
            }

            foreach (IProjectTree node in tree.Children)
            {
                if (node.IsFolder)
                {
                    IProjectTree result = await FindFileAsync(node).ConfigureAwait(false);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
