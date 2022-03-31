// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTreeStorage))]
    internal partial class PhysicalProjectTreeStorage : IPhysicalProjectTreeStorage
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectTreeService _treeService;
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly IActiveConfiguredValue<ConfiguredImports> _configuredImports;

        [ImportingConstructor]
        public PhysicalProjectTreeStorage(
            UnconfiguredProject project,
            [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]IProjectTreeService treeService,
            Lazy<IFileSystem> fileSystem,
            IActiveConfiguredValue<ConfiguredImports> configuredImports)
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

            _fileSystem.Value.Create(fullPath);

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
