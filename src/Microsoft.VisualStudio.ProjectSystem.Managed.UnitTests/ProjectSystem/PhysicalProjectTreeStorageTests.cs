// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Trait("UnitTest", "ProjectSystem")]
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
            var treeService = IProjectTreeServiceFactory.ImplementCurrentTree(() => null);
            var storage = CreateInstance(treeService: treeService);

            Assert.Throws<InvalidOperationException>(() => {

                var result = storage.CreateFolderAsync("path");
            });
        }

        [Fact]
        public async Task CreateFolderAsync_CreatesFolderOnDisk()
        {
            string result = null;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Root.csproj");
            var fileSystem = IFileSystemFactory.ImplementCreateDirectory((path) => { result = path; });

            var storage = CreateInstance(fileSystem: fileSystem, unconfiguredProject: unconfiguredProject);

            await storage.CreateFolderAsync("Folder");

            Assert.Equal(@"C:\Folder", result);
        }

        [Fact]
        public async Task CreateFolderAsync_IncludesFolderInProject()
        {
            string result = null;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Root.csproj");
            var folderManager = IFolderManagerFactory.IncludeFolderInProjectAsync((path, recursive) => { result = path; return Task.CompletedTask; });

            var storage = CreateInstance(folderManager: folderManager, unconfiguredProject: unconfiguredProject);

            await storage.CreateFolderAsync("Folder");

            Assert.Equal(@"C:\Folder", result);
        }

        [Fact]
        public async Task CreateFolderAsync_IncludesFolderInProjectNonRecusively()
        {
            bool? result = null;
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Root.csproj");
            var folderManager = IFolderManagerFactory.IncludeFolderInProjectAsync((path, recursive) => { result = recursive; return Task.CompletedTask; });

            var storage = CreateInstance(folderManager: folderManager, unconfiguredProject: unconfiguredProject);

            await storage.CreateFolderAsync("Folder");

            Assert.False(result);
        }

        [Theory]
        [InlineData(@"C:\Project.csproj",           @"Properties",                   @"C:\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties",                   @"C:\Projects\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties",                @"C:\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties",                @"C:\Properties")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties",                @"D:\Properties")]
        [InlineData(@"C:\Project.csproj",           @"Properties\Folder",            @"C:\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties\Folder",            @"C:\Projects\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties\Folder",         @"C:\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties\Folder",         @"C:\Properties\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties\Folder",         @"D:\Properties\Folder")]
        [InlineData(@"C:\Project.csproj",           @"Folder With Spaces",           @"C:\Folder With Spaces")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Folder With Spaces\Folder",    @"C:\Projects\Folder With Spaces\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Folder With Spaces\Folder", @"C:\Folder With Spaces\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Folder With Spaces\Folder", @"C:\Folder With Spaces\Folder")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Folder With Spaces\Folder", @"D:\Folder With Spaces\Folder")]
        public async Task CreateFolderAsync_ValueAsPath_IsCalculatedRelativeToProjectDirectory(string projectPath, string input, string expected)
        {
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectPath);
            string result = null;
            var treeProvider = IProjectTreeProviderFactory.ImplementFindByPath((root, path) => { result = path; return null; });
            var currentTree = ProjectTreeParser.Parse(projectPath);
            
            var storage = CreateInstance(treeProvider:treeProvider, unconfiguredProject: unconfiguredProject);

            await storage.CreateFolderAsync(input);

            Assert.Equal(expected, result);
        }

        private PhysicalProjectTreeStorage CreateInstance(IProjectTreeService treeService = null, IProjectTreeProvider treeProvider = null, IFileSystem fileSystem = null, IFolderManager folderManager = null, UnconfiguredProject unconfiguredProject = null)
        {
            treeService = treeService ?? IProjectTreeServiceFactory.Create(ProjectTreeParser.Parse("Root"));
            treeProvider = treeProvider ?? IProjectTreeProviderFactory.Create();
            fileSystem = fileSystem ?? IFileSystemFactory.Create();
            folderManager = folderManager ?? IFolderManagerFactory.Create();
            unconfiguredProject = unconfiguredProject ?? UnconfiguredProjectFactory.Create();

            return new PhysicalProjectTreeStorage(new Lazy<IProjectTreeService>(() => treeService),
                                                  new Lazy<IProjectTreeProvider>(() => treeProvider),
                                                  new Lazy<IFileSystem>(() => fileSystem),
                                                  ActiveConfiguredProjectFactory.ImplementValue(() => folderManager),
                                                  unconfiguredProject);
        }
    }
}
