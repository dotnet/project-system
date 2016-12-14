// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTreeStorage))]
    internal class PhysicalProjectTreeStorage : IPhysicalProjectTreeStorage
    {
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Lazy<IFolderManager> _folderManager;
        private readonly Lazy<IPhysicalProjectTree> _projectTree;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public PhysicalProjectTreeStorage(Lazy<IPhysicalProjectTree> projectTree, Lazy<IFileSystem> fileSystem, Lazy<IFolderManager> folderManager, UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(fileSystem, nameof(fileSystem));
            Requires.NotNull(folderManager, nameof(folderManager));
            Requires.NotNull(projectTree, nameof(projectTree));

            _fileSystem = fileSystem;
            _folderManager = folderManager;
            _projectTree = projectTree;
            _unconfiguredProject = unconfiguredProject;
        }

        public Task<IProjectTree> CreateFolderAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            if (_projectTree.Value.CurrentTree == null)
                throw new InvalidOperationException("Physical project tree has not yet been published.");

            string fullPath = _unconfiguredProject.MakeRooted(path);

            return AddToProjectAsync(fullPath);
        }

        private async Task<IProjectTree> AddToProjectAsync(string fullPath)
        {
            _fileSystem.Value.CreateDirectory(fullPath);

            await _folderManager.Value.IncludeFolderInProjectAsync(fullPath, recursive: false)
                                  .ConfigureAwait(false);

            IPhysicalProjectTree projectTree = _projectTree.Value;
            await projectTree.TreeService.PublishLatestTreeAsync()
                                         .ConfigureAwait(false);

            return projectTree.TreeProvider.FindByPath(projectTree.CurrentTree, fullPath);
        }
    }
}
