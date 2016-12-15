// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class PhysicalProjectTreeStorageTests
    {
        [Fact]
        public void CreateFolderAsync_NullAsPath_ThrowsArgumentNull()
        {
            var storage = CreateInstance();

            Assert.Throws<ArgumentNullException>("path", () => {

                var result = storage.CreateFolderAsync((string)null);
            });
        }

        [Fact]
        public void CreateFolderAsync_EmptyAsPath_ThrowsArgument()
        {
            var storage = CreateInstance();

            Assert.Throws<ArgumentException>("path", () => {

                var result = storage.CreateFolderAsync(string.Empty);
            });
        }

        [Fact]
        public void CreateFolderAsync_WhenTreeNotPublished_ThrowsInvalidOperation()
        {
            var physicalProjectTree = IPhysicalProjectTreeFactory.ImplementCurrentTree(() => null);
            var storage = CreateInstance(physicalProjectTree);

            Assert.Throws<InvalidOperationException>(() => {

                var result = storage.CreateFolderAsync("path");
            });
        }

        [Theory]
        [InlineData(@"C:\Project.csproj",           @"Properties",                 @"C:\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties",                 @"C:\Projects\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties",              @"C:\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties",              @"C:\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties",              @"D:\Properties")]
        [InlineData(@"C:\Project.csproj",           @"Properties\Folder",          @"C:\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties\Folder",          @"C:\Projects\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties\Folder",       @"C:\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties\Folder",       @"C:\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties\Folder",       @"D:\Properties\Folder")]

        public async Task CreateFolderAsync_ValueAsPath_IsCalculatedRelativeToProjectDirectory(string projectPath, string input, string expected)
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectPath);
            string result = null;
            var treeProvider = IProjectTreeProviderFactory.ImplementFindByPath((root, path) => { result = path; return null; });
            var currentTree = ProjectTreeParser.Parse(projectPath);
            var service = IProjectTreeServiceFactory.Create();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(treeProvider, currentTree, service);

            var storage = CreateInstance(physicalProjectTree: physicalProjectTree, unconfiguredProject: unconfiguredProject);

            await storage.CreateFolderAsync(input);

            Assert.Equal(expected, result);
        }

        private PhysicalProjectTreeStorage CreateInstance(IPhysicalProjectTree physicalProjectTree = null, IFileSystem fileSystem = null, IFolderManager folderManager = null, UnconfiguredProject unconfiguredProject = null)
        {
            physicalProjectTree = physicalProjectTree ?? IPhysicalProjectTreeFactory.Create();
            fileSystem = fileSystem ?? IFileSystemFactory.Create();
            folderManager = folderManager ?? IFolderManagerFactory.Create();
            unconfiguredProject = unconfiguredProject ?? IUnconfiguredProjectFactory.Create();

            return new PhysicalProjectTreeStorage(new Lazy<IPhysicalProjectTree>(() => physicalProjectTree),
                                             new Lazy<IFileSystem>(() => fileSystem),
                                             new Lazy<IFolderManager>(() => folderManager),
                                             unconfiguredProject);
        }
    }
}
