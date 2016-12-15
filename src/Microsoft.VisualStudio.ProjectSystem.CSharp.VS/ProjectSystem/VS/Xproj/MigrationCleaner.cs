// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal class MigrationCleaner
    {
        private readonly IMigrationCleanStore _migrationStore;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public MigrationCleaner(IMigrationCleanStore migrationStore, IFileSystem fileSystem)
        {
            _migrationStore = migrationStore;
            _fileSystem = fileSystem;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharp)]
        public Task CleanupFiles()
        {
            var toDelete = _migrationStore.DrainFiles();
            foreach (var file in toDelete)
            {
                _fileSystem.RemoveFile(file);
            }

            return Task.CompletedTask;
        }
    }
}
