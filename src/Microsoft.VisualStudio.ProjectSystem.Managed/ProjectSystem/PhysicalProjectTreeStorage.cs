// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTreeStorage))]
    internal partial class PhysicalProjectTreeStorage : IPhysicalProjectTreeStorage
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectTreeService _treeService;
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly ActiveConfiguredProject<ConfiguredImports> _configuredImports;

        [ImportingConstructor]
        public PhysicalProjectTreeStorage(
            UnconfiguredProject project,
            [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]IProjectTreeService treeService,
            Lazy<IFileSystem> fileSystem,
            ActiveConfiguredProject<ConfiguredImports> configuredImports)
        {
            _project = project;
            _treeService = treeService;
            _fileSystem = fileSystem;
            _configuredImports = configuredImports;
        }

        public async Task AddFileAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            string fullPath = _project.MakeRooted(path);

            await _configuredImports.Value.SourceItemsProvider.AddAsync(fullPath);

            await _treeService.PublishLatestTreeAsync(waitForFileSystemUpdates: false);
        }

        public async Task CreateEmptyFileAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            string fullPath = _project.MakeRooted(path);

            using (_fileSystem.Value.Create(fullPath)) { }

            await _configuredImports.Value.SourceItemsProvider.AddAsync(fullPath);

            await _treeService.PublishLatestTreeAsync(waitForFileSystemUpdates: true);
        }

        public Task CreateFolderAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            string fullPath = _project.MakeRooted(path);

            _fileSystem.Value.CreateDirectory(fullPath);

            return AddFolderAsync(fullPath);
        }

        public async Task AddFolderAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            string fullPath = _project.MakeRooted(path);

            await _configuredImports.Value.FolderManager.IncludeFolderInProjectAsync(fullPath, recursive: false);

            await _treeService.PublishLatestTreeAsync(waitForFileSystemUpdates: false);
        }
    }
}
