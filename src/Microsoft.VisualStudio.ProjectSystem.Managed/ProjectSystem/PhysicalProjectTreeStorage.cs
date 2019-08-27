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
        private readonly IProjectTreeService _treeService;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public PhysicalProjectTreeStorage([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]IProjectTreeService treeService,
                                          Lazy<IFileSystem> fileSystem,
                                          ActiveConfiguredProject<IFolderManager> folderManager,
                                          [Import(ExportContractNames.ProjectItemProviders.SourceFiles)]ActiveConfiguredProject<IProjectItemProvider> sourceItemProvider,
                                          UnconfiguredProject project)
        {
            _treeService = treeService;
            _sourceItemProvider = sourceItemProvider;
            _fileSystem = fileSystem;
            _folderManager = folderManager;
            _unconfiguredProject = project;
        }

        public async Task CreateEmptyFileAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            string fullPath = _unconfiguredProject.MakeRooted(path);

            using (_fileSystem.Value.Create(fullPath)) { }

            await _sourceItemProvider.Value.AddAsync(fullPath);

            await _treeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true);
        }

        public async Task CreateFolderAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            string fullPath = _unconfiguredProject.MakeRooted(path);

            _fileSystem.Value.CreateDirectory(fullPath);

            await _folderManager.Value.IncludeFolderInProjectAsync(fullPath, recursive: false);

            await _treeService.PublishLatestTreeAsync(waitForFileSystemUpdates: false);
        }
    }
}
