// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    /// Base type for special file providers.
    /// </summary>
    internal abstract class AbstractFindByNameSpecialFileProvider : AbstractSpecialFileProvider
    {
        private readonly ISpecialFilesManager _specialFilesManager;

        public AbstractFindByNameSpecialFileProvider(
            IPhysicalProjectTree projectTree,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
            IFileSystem fileSystem,
            ISpecialFilesManager specialFileManager)
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem)
        {
            Requires.NotNull(specialFileManager, nameof(specialFileManager));

            _specialFilesManager = specialFileManager;
        }

        /// <summary>
        /// If true, the special file is created under the app designer folder
        /// by default.
        /// </summary>
        protected virtual bool CreatedByDefaultUnderAppDesignerFolder => true;

        /// <summary>
        /// We follow this algorithm for looking up files:
        ///
        /// if (not asked to create)
        ///      Look in AppDesigner folder
        ///      Look in root folder
        ///
        /// if (asked to create)
        ///      Look in AppDesigner folder
        ///      Look in root folder
        ///      Force-create in app-designer folder unless that file is not created there by default.
        ///      In that case create under the root node.
        /// </summary>

        /// <summary>
        /// Find a file with the given file name. The algorithm used is :
        ///       Look under the appdesigner folder for files that normally live there.
        ///       Look under the project root for all files.
        /// </summary>
        protected override async Task<IProjectTree> FindFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default)
        {
            IProjectTree rootNode = ProjectTree.CurrentTree;
            IProjectTree specialFileNode;

            // First, we look in the AppDesigner folder.
            if (CreatedByDefaultUnderAppDesignerFolder)
            {
                IProjectTree appDesignerFolder = await GetAppDesignerFolderAsync(createIfNotExists: false).ConfigureAwait(false);
                if (appDesignerFolder != null)
                {
                    specialFileNode = FindFileWithinNode(appDesignerFolder, Name);
                    if (specialFileNode != null)
                    {
                        return specialFileNode;
                    }
                }
            }

            // Now try the root folder.
            specialFileNode = FindFileWithinNode(rootNode, Name);
            if (specialFileNode != null)
            {
                return specialFileNode;
            }

            return null;
        }

        /// <summary>
        /// Create a special file.
        /// </summary>
        protected override async Task<string> CreateFileAsync(
            SpecialFiles fileId,
            SpecialFileFlags flags,
            CancellationToken cancellationToken = default)
        {
            IProjectTree rootNode = await GetParentFolderAsync(createIfNotExists: true).ConfigureAwait(false);
            return await CreateFileAsync(rootNode).ConfigureAwait(false);
        }

        /// <summary>
        /// Find a file with the given filename within the given node.
        /// </summary>
        private static IProjectTree FindFileWithinNode(IProjectTree parentNode, string fileName)
        {
            parentNode.TryFindImmediateChild(fileName, out IProjectTree fileNode);

            // The user has created a folder with this name which means we don't have a special file.
            if (fileNode != null && fileNode.IsFolder)
            {
                return null;
            }

            return fileNode;
        }

        private async Task<IProjectTree> GetParentFolderAsync(bool createIfNotExists)
        {
            if (CreatedByDefaultUnderAppDesignerFolder)
            {
                IProjectTree tree = await GetAppDesignerFolderAsync(createIfNotExists).ConfigureAwait(false);
                if (tree != null)
                    return tree;
            }

            return ProjectTree.CurrentTree;
        }

        private async Task<IProjectTree> GetAppDesignerFolderAsync(bool createIfNotExists)
        {
            SpecialFileFlags flags = SpecialFileFlags.FullPath;
            if (createIfNotExists)
                flags |= SpecialFileFlags.CreateIfNotExist;

            string path = await _specialFilesManager.GetFileAsync(SpecialFiles.AppDesigner, flags)
                                                    .ConfigureAwait(false);
            if (path == null)
                return null;

            return ProjectTree.TreeProvider.FindByPath(ProjectTree.CurrentTree, path);
        }
    }
}
