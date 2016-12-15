// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    /// Base type for special file providers.
    /// </summary>
    internal abstract class AbstractSpecialFileProvider : ISpecialFileProvider
    {
        private readonly IProjectTreeService _projectTreeService;
        private readonly IProjectItemProvider _sourceItemsProvider;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// A service that knows to create a file from a template. In non-VS scenarios
        /// this might not exist, in which case we provide a default implementation for adding
        /// a file.
        /// </summary>
        private readonly Lazy<ICreateFileFromTemplateService> _templateFileCreationService;

        public AbstractSpecialFileProvider([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)] IProjectTreeService projectTreeService,
                                           [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                           [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                           IFileSystem fileSystem)
        {
            Requires.NotNull(projectTreeService, nameof(projectTreeService));
            Requires.NotNull(sourceItemsProvider, nameof(sourceItemsProvider));
            Requires.NotNull(fileSystem, nameof(fileSystem));

            _projectTreeService = projectTreeService;
            _sourceItemsProvider = sourceItemsProvider;
            _templateFileCreationService = templateFileCreationService;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// If true, the special file is created under the app designer folder
        /// by default.
        /// </summary>
        protected virtual bool CreatedByDefaultUnderAppDesignerFolder => true;

        /// <summary>
        /// Gets the name of a special file.
        /// </summary>
        protected abstract string Name
        {
            get;
        }

        /// <summary>
        /// Gets the name of a template that can be used to create a new special file.
        /// </summary>
        protected abstract string TemplateName
        {
            get;
        }

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
        public async Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Search for the file in the app designer and root folders.
            IProjectTree specialFileNode = FindFile(Name);
            if (specialFileNode != null)
            {
                if (await IsNodeInSyncWithDiskAsync(specialFileNode, forceSync: flags.HasFlag(SpecialFileFlags.CreateIfNotExist), cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    return specialFileNode.FilePath;
                }
            }

            // File doesn't exist. Create it if we've been asked to.
            if (flags.HasFlag(SpecialFileFlags.CreateIfNotExist))
            {
                string createdFilePath = await CreateFileAsync(fileId, Name).ConfigureAwait(false);
                if (createdFilePath != null)
                {
                    return createdFilePath;
                }
            }

            // We haven't found the file but return the default file path as that's the contract.
            IProjectTree rootNode = _projectTreeService.CurrentTree.Tree;
            string rootFilePath = _projectTreeService.CurrentTree.TreeProvider.GetPath(rootNode);
            string fullPath = Path.Combine(Path.GetDirectoryName(rootFilePath), Name);
            return fullPath;
        }

        /// <summary>
        /// Find a file with the given file name. The algorithm used is :
        ///       Look under the appdesigner folder for files that normally live there.
        ///       Look under the project root for all files.
        /// </summary>
        private IProjectTree FindFile(string specialFileName)
        {
            IProjectTree rootNode = _projectTreeService.CurrentTree.Tree;
            IProjectTree specialFileNode;

            // First, we look in the AppDesigner folder.
            IProjectTree appDesignerFolder = GetAppDesignerFolder();
            if (appDesignerFolder != null && CreatedByDefaultUnderAppDesignerFolder)
            {
                specialFileNode = FindFileWithinNode(appDesignerFolder, specialFileName);
                if (specialFileNode != null)
                {
                    return specialFileNode;
                }
            }

            // Now try the root folder.
            specialFileNode = FindFileWithinNode(rootNode, specialFileName);
            if (specialFileNode != null)
            {
                return specialFileNode;
            }

            return null;
        }

        /// <summary>
        /// Find a file with the given filename within the given node.
        /// </summary>
        private IProjectTree FindFileWithinNode(IProjectTree parentNode, string fileName)
        {
            IProjectTree fileNode;
            parentNode.TryFindImmediateChild(fileName, out fileNode);

            // The user has created a folder with this name which means we don't have a special file.
            if (fileNode != null && fileNode.IsFolder)
            {
                return null;
            }

            return fileNode;
        }

        /// <summary>
        /// Check to see if a given node is both part of a project and exists on disk. If asked to 
        /// force sync, then either a file is included in the project or removed from the project.
        /// </summary>
        private async Task<bool> IsNodeInSyncWithDiskAsync(IProjectTree specialFileNode, bool forceSync, CancellationToken cancellationToken)
        {
            // If the file exists on disk but is not part of the project.
            if (!specialFileNode.Flags.IsIncludedInProject())
            {
                if (forceSync)
                {
                    // Since the file already exists on disk, just include it in the project.
                    await _sourceItemsProvider.AddAsync(specialFileNode.FilePath).ConfigureAwait(false);
                    await _projectTreeService.PublishLatestTreeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return false;
                }
            }

            // If the file was in the project but not on disk.
            if (!_fileSystem.FileExists(specialFileNode.FilePath))
            {
                if (forceSync)
                {
                    // Just remove the entry from the project so that we get to a clean state and then we can 
                    // create the file as usual.
                    await _projectTreeService.CurrentTree.TreeProvider.RemoveAsync(ImmutableHashSet.Create(specialFileNode)).ConfigureAwait(false);
                    await _projectTreeService.PublishLatestTreeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Create a special file.
        /// </summary>
        private async Task<string> CreateFileAsync(SpecialFiles fileId, string specialFileName)
        {
            IProjectTree rootNode = _projectTreeService.CurrentTree.Tree;
            IProjectTree appDesignerFolder = GetAppDesignerFolder();
            if (appDesignerFolder == null && CreatedByDefaultUnderAppDesignerFolder)
            {
                return null;
            }

            var parentNode = CreatedByDefaultUnderAppDesignerFolder ? appDesignerFolder : rootNode;

            bool fileCreated = false;

            var parentPath = _projectTreeService.CurrentTree.TreeProvider.GetPath(parentNode);
            var specialFilePath = Path.Combine(
                parentNode.IsFolder ? parentPath : Path.GetDirectoryName(parentPath),
                specialFileName);

            // If we can create the file from the template do it, otherwise just create an empty file.
            if (_templateFileCreationService != null)
            {
                fileCreated = await _templateFileCreationService.Value.CreateFileAsync(TemplateName, parentPath, specialFileName).ConfigureAwait(false);
            }
            else
            {
                using (_fileSystem.Create(specialFilePath)) { }

                IProjectItem item = await _sourceItemsProvider.AddAsync(specialFilePath).ConfigureAwait(false);
                if (item != null)
                {
                    fileCreated = true;
                    await _projectTreeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true).ConfigureAwait(false);
                }
            }

            return specialFilePath;
        }

        private IProjectTree GetAppDesignerFolder()
        {
            IProjectTree rootNode = _projectTreeService.CurrentTree.Tree;

            return rootNode.Children.FirstOrDefault(child => child.IsFolder && child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
        }
    }
}
