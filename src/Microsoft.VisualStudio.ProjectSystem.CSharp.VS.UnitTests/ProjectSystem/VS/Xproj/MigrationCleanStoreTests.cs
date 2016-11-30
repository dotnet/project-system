// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [ProjectSystemTrait]
    public class MigrationCleanStoreTests
    {
        [Fact]
        public void MigrationCleanStore_AddFiles_AddsFilesToSet()
        {
            var store = new MigrationCleanStore();
            store.AddFiles(@"C:\file1.csproj", @"C:\file1.csproj", @"C:\file2.csproj");
            Assert.Equal(2, store.Files.Count);
            Assert.Contains(@"C:\file1.csproj", store.Files);
            Assert.Contains(@"C:\file2.csproj", store.Files);
        }

        [Fact]
        public void MigrationCleanStore_DrainFiles_RemovesAllFiles()
        {
            var store = new MigrationCleanStore();
            store.Files.Add(@"C:\file.csproj");
            var files = store.DrainFiles();
            Assert.Equal(1, files.Count);
            Assert.Contains(@"C:\file.csproj", files);
            Assert.Empty(store.Files);
        }
    }
}
