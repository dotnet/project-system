// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public class AppDesignerFolderSpecialFileProviderTests
    {
        [Theory]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\Properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                properties (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                PROPERTIES (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\PROPERTIES")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Properties (flags: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\Properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
                    AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
            """,
            @"C:\Project\Properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
                    AssemblyInfo.cs (flags: {})
            """,
            @"C:\Project\Properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder (flags: {Folder})
            """,
            @"C:\Project\Properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Folder (flags: {Folder})
                    Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\Folder\Properties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Folder (flags: {Folder})
                    NotCalledProperties (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\Folder\NotCalledProperties")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Folder (flags: {Folder})
                    My Project (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\Folder\My Project")]
        [InlineData(
            """
            Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                Folder1 (flags: {Folder})
                Folder2 (flags: {Folder})
                    My Project (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            @"C:\Project\Folder2\My Project")]
        public async Task GetFile_WhenTreeWithAppDesignerFolder_ReturnsPath(string input, string expected)
        {
            var tree = ProjectTreeParser.Parse(input);
            var treeProvider = new ProjectTreeProvider();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(provider: treeProvider, currentTree: tree);

            var provider = CreateInstance(physicalProjectTree);

            var result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.FullPath);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetFile_WhenTreeWithAppDesignerFolder_ReturnsPathIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    My Project (flags: {Folder AppDesignerFolder BubbleUp})
                """);

            var treeProvider = new ProjectTreeProvider();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(provider: treeProvider, currentTree: tree);

            var provider = CreateInstance(physicalProjectTree);

            var result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(@"C:\Project\My Project", result);
        }

        [Theory]    // AppDesignerFolder        // Expected return
        [InlineData(@"Properties",              @"C:\Project\Properties")]
        [InlineData(@"My Project",              @"C:\Project\My Project")]
        [InlineData(@"Folder\AppDesigner",      @"C:\Project\Folder\AppDesigner")]
        [InlineData(@"",                        null)]
        public async Task GetFile_WhenTreeWithoutAppDesignerFolder_ReturnsDefaultAppDesignerFolder(string input, string expected)
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity Folder})
                """);

            var treeProvider = new ProjectTreeProvider();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(provider: treeProvider, currentTree: tree);

            var projectProperties = CreateProperties(appDesignerFolderName: input);

            var provider = CreateInstance(physicalProjectTree, projectProperties);

            var result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.FullPath);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithFileSameName_ReturnsDefaultAppDesignerFolder()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity FileOnDisk})
                """);

            var treeProvider = new ProjectTreeProvider();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(provider: treeProvider, currentTree: tree);

            var projectProperties = CreateProperties(appDesignerFolderName: "Properties");

            var provider = CreateInstance(physicalProjectTree, projectProperties);

            var result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.FullPath);

            Assert.Equal(@"C:\Project\Properties", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithFileSameName_ThrowsIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity FileOnDisk}), FilePath: "C:\Project\Properties"
                """);

            var treeProvider = new ProjectTreeProvider();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(provider: treeProvider, currentTree: tree);

            var projectProperties = CreateProperties(appDesignerFolderName: "Properties");

            var provider = CreateInstance(physicalProjectTree, projectProperties);

            await Assert.ThrowsAsync<IOException>(() =>
            {
                return provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);
            });
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithExcludedFolder_IsAddedToProjectIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity Folder IncludeInProjectCandidate}), FilePath: "C:\Project\Properties"
                """);
            int callCount = 0;
            var storage = IPhysicalProjectTreeStorageFactory.ImplementAddFolderAsync(path => callCount++);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);

            var provider = CreateInstance(physicalProjectTree);

            await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithExistentAppDesignerFolder_ReturnsPathIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity Folder AppDesignerFolder})
                """);

            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree);

            var provider = CreateInstance(physicalProjectTree);

            string? result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(@"C:\Project\Properties", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithNoAppDesignerFolder_IsCreatedIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                """);

            int callCount = 0;
            var storage = IPhysicalProjectTreeStorageFactory.ImplementCreateFolderAsync(path => callCount++);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);

            var projectProperties = CreateProperties(appDesignerFolderName: "Properties");

            var provider = CreateInstance(physicalProjectTree, projectProperties);

            await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithMissingAppDesignerFolder_IsCreatedIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {Folder AppDesignerFolder})
                """);
            int callCount = 0;
            var storage = IPhysicalProjectTreeStorageFactory.ImplementCreateFolderAsync(path => callCount++);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);

            var provider = CreateInstance(physicalProjectTree);

            await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetFileAsync_WhenRootMarkedWithDisableAddItemFolder_ReturnsNull()
        {   // Mimics an extension turning on DisableAddItem flag for our parent
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot DisableAddItemFolder}), FilePath: "C:\Project\Project.csproj"
                """);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree);

            var provider = CreateInstance(physicalProjectTree);

            var result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.CreateIfNotExist);

            Assert.Null(result);
        }

        private static ProjectProperties CreateProperties(string appDesignerFolderName)
        {
            return ProjectPropertiesFactory.Create(UnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData(AppDesigner.SchemaName, AppDesigner.FolderNameProperty, appDesignerFolderName)
                });
        }

        private static AppDesignerFolderSpecialFileProvider CreateInstance(IPhysicalProjectTree? physicalProjectTree = null, ProjectProperties? properties = null)
        {
            physicalProjectTree ??= IPhysicalProjectTreeFactory.Create();
            properties ??= CreateProperties(appDesignerFolderName: "Properties");

            return new AppDesignerFolderSpecialFileProvider(physicalProjectTree, properties);
        }
    }
}
