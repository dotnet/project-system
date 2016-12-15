// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Mocks;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [ProjectSystemTrait]
    public class MigrationCleanerTests
    {
        [Fact]
        public async Task MigrationCleaner_GivenFilesToClean_RemovesFiles()
        {
            var store = new MigrationCleanStore();
            store.AddFiles(@"C:\project1\project1.xproj", @"C:\project1\project.json");
            var fileSystemMock = new IFileSystemMock();
            fileSystemMock.Create(@"C:\project1\project1.xproj");
            fileSystemMock.Create(@"C:\project1\project.json");

            var cleaner = new MigrationCleaner(store, fileSystemMock);
            await cleaner.CleanupFiles();
            Assert.False(fileSystemMock.FileExists(@"C:\project1\project1.xproj"));
            Assert.False(fileSystemMock.FileExists(@"C:\project1\project.json"));
        }

        [Fact]
        public async Task MigrationCleaner_NoFilesToClean_DoesNotCallRemoveFile()
        {
            var store = new MigrationCleanStore();
            var fileSystem = IFileSystemFactory.Create();
            var cleaner = new MigrationCleaner(store, fileSystem);
            await cleaner.CleanupFiles();
            Mock.Get(fileSystem).Verify(f => f.RemoveFile(It.IsAny<string>()), Times.Never);
        }
    }
}
