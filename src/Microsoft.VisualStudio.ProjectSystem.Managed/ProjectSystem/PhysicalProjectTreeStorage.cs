// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTreeStorage))]
    internal class PhysicalProjectTreeStorage : IPhysicalProjectTreeStorage
    {
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly ActiveConfiguredProject<IFolderManager> _folderManager;
        private readonly ActiveConfiguredProject<IProjectItemProvider> _sourceItemProvider;
        private readonly Lazy<IProjectTreeService> _treeService;
        private readonly Lazy<IProjectTreeProvider> _treeProvider;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public PhysicalProjectTreeStorage([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]Lazy<IProjectTreeService> treeService,
                                          [Import(ExportContractNames.ProjectTreeProviders.PhysicalViewTree)]Lazy<IProjectTreeProvider> treeProvider,
                                          Lazy<IFileSystem> fileSystem,
                                          ActiveConfiguredProject<IFolderManager> folderManager,
                                          [Import(ExportContractNames.ProjectItemProviders.SourceFiles)]ActiveConfiguredProject<IProjectItemProvider> sourceItemProvider,
                                          UnconfiguredProject project)
        {
            _treeService = treeService;
            _treeProvider = treeProvider;
            _sourceItemProvider = sourceItemProvider;
            _fileSystem = fileSystem;
            _folderManager = folderManager;
            _unconfiguredProject = project;
        }

        public async Task<IProjectItem?> CreateFileAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            return await AddToProjectAsync(path, waitForFileSystemUpdates: true, async fullPath =>
            {
                using (_fileSystem.Value.Create(fullPath)) { }

                await _sourceItemProvider.Value.AddAsync(fullPath);

            }) as IProjectItem;
        }

        public Task<IProjectTree?> CreateFolderAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            return AddToProjectAsync(path, waitForFileSystemUpdates: false, async fullPath =>
            {
                _fileSystem.Value.CreateDirectory(fullPath);

                await _folderManager.Value.IncludeFolderInProjectAsync(fullPath, recursive: false);
            });
        }

        private async Task<IProjectTree?> AddToProjectAsync(string path, bool waitForFileSystemUpdates, Func<string, Task> addToProject)
        {
            string fullPath = _unconfiguredProject.MakeRooted(path);

            await addToProject(fullPath);

            await _treeService.Value.PublishLatestTreeAsync(waitForFileSystemUpdates);

            return _treeProvider.Value.FindByPath(_treeService.Value.CurrentTree.Tree, fullPath);
        }
    }
}
