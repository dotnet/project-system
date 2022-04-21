// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq.Protected;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public abstract class AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase
    {
        private readonly string _fileName;

        protected AbstractFindByNameUnderAppDesignerSpecialFileProviderTestBase(string fileName)
        {
            _fileName = fileName;
        }

        [Fact]
        public async Task GetFileAsync_WhenNoAppDesigner_ReturnsPathUnderAppDesigner()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                """);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(@"C:\Project\Properties");
            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\Properties\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenAppDesigner_ReturnsPathUnderAppDesigner()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity Folder AppDesignerFolder}), FilePath: "C:\Project\Properties"
                """);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(@"C:\Project\Properties");
            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\Properties\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenAppDesignerNotSupported_ReturnsPathUnderProjectRoot()
        {   // AppDesigner is turned off
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                """);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);

            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenAppDesignerWithFile_ReturnsPath()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity Folder AppDesignerFolder}), FilePath: "C:\Project\Properties"
                        {{_fileName}} (flags: {FileSystemEntity FileOnDisk}), FilePath: "C:\Project\Properties\{{_fileName}}"
                """);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(@"C:\Project\Properties");
            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\Properties\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenAppDesignerButRootWithFile_ReturnsPath()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    Properties (flags: {FileSystemEntity Folder AppDesignerFolder}), FilePath: "C:\Project\Properties"
                    {{_fileName}} (flags: {FileSystemEntity FileOnDisk}), FilePath: "C:\Project\{{_fileName}}"
                """);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(@"C:\Project\Properties");
            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenRootWithFile_ReturnsPath()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    {{_fileName}} (flags: {FileSystemEntity FileOnDisk}), FilePath: "C:\Project\{{_fileName}}"
                """);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);

            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithFolderSameName_ReturnsPath()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    {{_fileName}} (flags: {FileSystemEntity Folder}), FilePath: "C:\Project\{{_fileName}}"
                """);

            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);
            var provider = CreateInstance(specialFilesManager, tree);

            var result = await provider.GetFileAsync(0, SpecialFileFlags.FullPath);

            Assert.Equal($@"C:\Project\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithFolderSameName_ThrowsIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    {{_fileName}} (flags: {FileSystemEntity Folder}), FilePath: "C:\Project\{{_fileName}}"
                """);

            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);
            var provider = CreateInstance(specialFilesManager, tree);

            await Assert.ThrowsAsync<IOException>(() =>
            {
                return provider.GetFileAsync(0, SpecialFileFlags.CreateIfNotExist);
            });
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithExcludedFile_IsAddedToProjectIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    {{_fileName}} (flags: {FileSystemEntity FileOnDisk IncludeInProjectCandidate}), FilePath: "C:\Project\{{_fileName}}"
                """);
            int callCount = 0;
            var storage = IPhysicalProjectTreeStorageFactory.ImplementAddFileAsync(path => callCount++);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);
            var provider = CreateInstance(specialFilesManager, physicalProjectTree);

            await provider.GetFileAsync(0, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithExistentFile_ReturnsPathIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    {{_fileName}} (flags: {FileSystemEntity FileOnDisk}), FilePath: "C:\Project\{{_fileName}}"
                """);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);
            var provider = CreateInstance(specialFilesManager, physicalProjectTree);

            string? result = await provider.GetFileAsync(0, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal($@"C:\Project\{_fileName}", result);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithNoFile_IsCreatedIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                """);

            int callCount = 0;
            var storage = IPhysicalProjectTreeStorageFactory.ImplementCreateEmptyFileAsync(path => callCount++);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);
            var provider = CreateInstance(specialFilesManager, physicalProjectTree);

            await provider.GetFileAsync(0, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetFileAsync_WhenTreeWithMissingFile_IsCreatedIfCreateIfNotExist()
        {
            var tree = ProjectTreeParser.Parse(
                $$"""
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                    {{_fileName}} (flags: {}), FilePath: "C:\Project\{{_fileName}}"
                """);
            int callCount = 0;
            var storage = IPhysicalProjectTreeStorageFactory.ImplementCreateEmptyFileAsync(path => callCount++);
            var specialFilesManager = ISpecialFilesManagerFactory.ImplementGetFile(null);
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);
            var provider = CreateInstance(specialFilesManager, physicalProjectTree);

            await provider.GetFileAsync(0, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        private AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IProjectTree projectTree)
        {
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: projectTree);

            return CreateInstance(specialFilesManager, physicalProjectTree);
        }

        internal abstract AbstractFindByNameUnderAppDesignerSpecialFileProvider CreateInstance(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree);

        internal static T CreateInstanceWithOverrideCreateFileAsync<T>(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree, params object[] additionalArguments)
            where T : AbstractFindByNameUnderAppDesignerSpecialFileProvider
        {
            object[] arguments = new object[3 + additionalArguments.Length];
            arguments[0] = specialFilesManager;
            arguments[1] = projectTree;
            arguments[2] = null!;
            additionalArguments.CopyTo(arguments, 3);

            // We override CreateFileAsync to call the CreateEmptyFileAsync which makes writting tests in the base easier
            var mock = new Mock<T>(arguments);
            mock.Protected().Setup<Task>("CreateFileCoreAsync", ItExpr.IsAny<string>())
                .Returns<string>(projectTree.TreeStorage.CreateEmptyFileAsync);

            mock.CallBase = true;

            return mock.Object;
        }
    }
}
