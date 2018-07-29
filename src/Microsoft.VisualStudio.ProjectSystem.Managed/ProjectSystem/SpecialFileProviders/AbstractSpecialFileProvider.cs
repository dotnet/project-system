using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    internal abstract class AbstractSpecialFileProvider : ISpecialFileProvider
    {
        protected AbstractSpecialFileProvider(
            IPhysicalProjectTree projectTree,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
            IFileSystem fileSystem)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(sourceItemsProvider, nameof(sourceItemsProvider));
            Requires.NotNull(fileSystem, nameof(fileSystem));

            ProjectTree = projectTree;
            SourceItemsProvider = sourceItemsProvider;
            TemplateFileCreationService = templateFileCreationService;
            FileSystem = fileSystem;
        }

        protected IPhysicalProjectTree ProjectTree { get; }
        protected IProjectItemProvider SourceItemsProvider { get; }

        /// <summary>
        /// A service that knows to create a file from a template. In non-VS scenarios
        /// this might not exist, in which case we provide a default implementation for adding
        /// a file.
        /// </summary>
        protected Lazy<ICreateFileFromTemplateService> TemplateFileCreationService { get; }

        protected IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets the name of a special file.
        /// </summary>
        protected abstract string Name { get; }

        /// <summary>
        /// Gets the name of a template that can be used to create a new special file.
        /// </summary>
        protected abstract string TemplateName { get; }

        public async Task<string> GetFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default)
        {
            IProjectTree specialFileNode = await FindFileAsync(fileId, flags, cancellationToken).ConfigureAwait(false);
            bool createIfNotExists = flags.HasFlag(SpecialFileFlags.CreateIfNotExist);

            if (specialFileNode != null)
            {
                if (await IsNodeInSyncWithDiskAsync(
                    specialFileNode,
                    forceSync: createIfNotExists,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false))
                {
                    return specialFileNode.FilePath;
                }
            }

            if (createIfNotExists)
            {
                string specialFilePath = await CreateFileAsync(fileId, flags, cancellationToken).ConfigureAwait(false);

                if (specialFilePath != null)
                {
                    return specialFilePath;
                }
            }

            // We haven't found the file but return the default file path as that's the contract.
            IProjectTree rootNode = ProjectTree.CurrentTree;
            string rootFilePath = ProjectTree.TreeProvider.GetPath(rootNode);
            string fullPath = Path.Combine(Path.GetDirectoryName(rootFilePath), Name);
            return fullPath;
        }

        protected abstract Task<IProjectTree> FindFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default);

        protected abstract Task<string> CreateFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default);

        protected async Task<string> CreateFileAsync(IProjectTree targetTree)
        {
            string parentPath = ProjectTree.TreeProvider.GetRootedAddNewItemDirectory(targetTree);
            string specialFilePath = Path.Combine(parentPath, Name);

            // If we can create the file from the template do it, otherwise just create an empty file.
            if (TemplateFileCreationService != null)
            {
                await TemplateFileCreationService.Value.CreateFileAsync(TemplateName, parentPath, Name).ConfigureAwait(false);
            }
            else
            {
                using (FileSystem.Create(specialFilePath))
                { }

                IProjectItem item = await SourceItemsProvider.AddAsync(specialFilePath).ConfigureAwait(false);
                if (item != null)
                {
                    await ProjectTree.TreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(false);
                }
            }

            return specialFilePath;
        }

        /// <summary>
        /// Check to see if a given node is both part of a project and exists on disk. If asked to 
        /// force sync, then either a file is included in the project or removed from the project.
        /// </summary>
        private async Task<bool> IsNodeInSyncWithDiskAsync(
            IProjectTree specialFileNode,
            bool forceSync,
            CancellationToken cancellationToken)
        {
            // If the file exists on disk but is not part of the project.
            if (!specialFileNode.Flags.IsIncludedInProject())
            {
                if (forceSync)
                {
                    // Since the file already exists on disk, just include it in the project.
                    await SourceItemsProvider.AddAsync(specialFileNode.FilePath).ConfigureAwait(false);
                    await ProjectTree.TreeService.PublishLatestTreeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return false;
                }
            }

            // If the file was in the project but not on disk.
            if (!FileSystem.FileExists(specialFileNode.FilePath))
            {
                if (forceSync)
                {
                    // Just remove the entry from the project so that we get to a clean state and then we can 
                    // create the file as usual.
                    await ProjectTree.TreeProvider.RemoveAsync(ImmutableHashSet.Create(specialFileNode)).ConfigureAwait(false);
                    await ProjectTree.TreeService.PublishLatestTreeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return false;
            }

            return true;
        }
    }
}
