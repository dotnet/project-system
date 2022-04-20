// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq.Protected;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    public abstract class AbstractFindByNameSpecialFileProviderTestBase
    {
        private readonly string _fileName;

        protected AbstractFindByNameSpecialFileProviderTestBase(string fileName)
        {
            _fileName = fileName;
        }

        [Fact]
        public async Task GetFileAsync_ReturnsPathUnderProjectRoot()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Project (flags: {ProjectRoot}), FilePath: "C:\Project\Project.csproj"
                """);
            var provider = CreateInstance(tree);

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
            var provider = CreateInstance(tree);

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

            var provider = CreateInstance(tree);

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

            var provider = CreateInstance(tree);

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
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);
            var provider = CreateInstance(physicalProjectTree);

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
            var provider = CreateInstance(physicalProjectTree);

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
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);
            var provider = CreateInstance(physicalProjectTree);

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
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: tree, storage: storage);
            var provider = CreateInstance(physicalProjectTree);

            await provider.GetFileAsync(0, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(1, callCount);
        }

        internal AbstractFindByNameSpecialFileProvider CreateInstance(IProjectTree projectTree)
        {
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(currentTree: projectTree);

            return CreateInstance(physicalProjectTree);
        }

        internal abstract AbstractFindByNameSpecialFileProvider CreateInstance(IPhysicalProjectTree projectTree);

        internal static T CreateInstanceWithOverrideCreateFileAsync<T>(IPhysicalProjectTree projectTree, params object[] args)
            where T : AbstractFindByNameSpecialFileProvider
        {
            object[] arguments = new object[args.Length + 1];
            arguments[0] = projectTree;
            args.CopyTo(arguments, 1);

            // We override CreateFileAsync to call the CreateEmptyFileAsync which makes writting tests in the base easier
            var mock = new Mock<T>(arguments);
            mock.Protected().Setup<Task>("CreateFileAsync", ItExpr.IsAny<string>())
                .Returns<string>(projectTree.TreeStorage.CreateEmptyFileAsync);

            mock.CallBase = true;

            return mock.Object;
        }
    }
}
