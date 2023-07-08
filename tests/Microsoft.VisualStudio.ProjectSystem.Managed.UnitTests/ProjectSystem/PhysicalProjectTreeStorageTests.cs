// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class PhysicalProjectTreeStorageTests
    {
        [Fact]
        public async Task AddFileAsync_NullAsPath_ThrowsArgumentNull()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("path", () =>
            {
                return storage.AddFileAsync(null!);
            });
        }

        [Fact]
        public async Task AddFileAsync_EmptyAsPath_ThrowsArgument()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("path", () =>
            {
                return storage.AddFileAsync(string.Empty);
            });
        }

        [Fact]
        public async Task CreateEmptyFileAsync_NullAsPath_ThrowsArgumentNull()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("path", () =>
            {
                return storage.CreateEmptyFileAsync(null!);
            });
        }

        [Fact]
        public async Task CreateEmptyFileAsync_EmptyAsPath_ThrowsArgument()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("path", () =>
            {
                return storage.CreateEmptyFileAsync(string.Empty);
            });
        }

        [Fact]
        public async Task CreateFolderAsync_NullAsPath_ThrowsArgumentNull()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("path", async () =>
            {
                await storage.CreateFolderAsync(null!);
            });
        }

        [Fact]
        public async Task CreateFolderAsync_EmptyAsPath_ThrowsArgument()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("path", async () =>
            {
                await storage.CreateFolderAsync(string.Empty);
            });
        }

        [Fact]
        public async Task AddFolderAsync_NullAsPath_ThrowsArgumentNull()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("path", async () =>
            {
                await storage.AddFolderAsync(null!);
            });
        }

        [Fact]
        public async Task AddFolderAsync_EmptyAsPath_ThrowsArgument()
        {
            var storage = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("path", async () =>
            {
                await storage.AddFolderAsync(string.Empty);
            });
        }

        [Fact]
        public async Task CreateEmptyFileAsync_CreatesFileOnDisk()
        {
            string? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Project\Project.csproj");
            var fileSystem = IFileSystemFactory.ImplementCreate((path) => { result = path; });

            var storage = CreateInstance(fileSystem: fileSystem, project: project);

            await storage.CreateEmptyFileAsync(@"Properties\File.cs");

            Assert.Equal(@"C:\Project\Properties\File.cs", result);
        }

        [Fact]
        public async Task CreateFolderAsync_CreatesFolderOnDisk()
        {
            string? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Root.csproj");
            var fileSystem = IFileSystemFactory.ImplementCreateDirectory((path) => { result = path; });

            var storage = CreateInstance(fileSystem: fileSystem, project: project);

            await storage.CreateFolderAsync("Folder");

            Assert.Equal(@"C:\Folder", result);
        }

        [Fact]
        public async Task CreateEmptyFileAsync_AddsFileToProject()
        {
            string? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Project.csproj");

            var sourceItemsProvider = IProjectItemProviderFactory.AddItemAsync(path => { result = path; return null!; });
            var storage = CreateInstance(sourceItemsProvider: sourceItemsProvider, project: project);

            await storage.CreateEmptyFileAsync("File.cs");

            Assert.Equal(@"C:\File.cs", result);
        }

        [Fact]
        public async Task AddFileAsync_AddsFileToProject()
        {
            string? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Project.csproj");

            var sourceItemsProvider = IProjectItemProviderFactory.AddItemAsync(path => { result = path; return null!; });
            var storage = CreateInstance(sourceItemsProvider: sourceItemsProvider, project: project);

            await storage.AddFileAsync("File.cs");

            Assert.Equal(@"C:\File.cs", result);
        }

        [Fact]
        public async Task CreateFolderAsync_IncludesFolderInProject()
        {
            string? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Root.csproj");
            var folderManager = IFolderManagerFactory.IncludeFolderInProjectAsync((path, recursive) => { result = path; return Task.CompletedTask; });

            var storage = CreateInstance(folderManager: folderManager, project: project);

            await storage.CreateFolderAsync("Folder");

            Assert.Equal(@"C:\Folder", result);
        }

        [Fact]
        public async Task AddFolderAsync_IncludesFolderInProject()
        {
            string? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Root.csproj");
            var folderManager = IFolderManagerFactory.IncludeFolderInProjectAsync((path, recursive) => { result = path; return Task.CompletedTask; });

            var storage = CreateInstance(folderManager: folderManager, project: project);

            await storage.AddFolderAsync("Folder");

            Assert.Equal(@"C:\Folder", result);
        }

        [Fact]
        public async Task CreateFolderAsync_IncludesFolderInProjectNonRecursively()
        {
            bool? result = null;
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Root.csproj");
            var folderManager = IFolderManagerFactory.IncludeFolderInProjectAsync((path, recursive) => { result = recursive; return Task.CompletedTask; });

            var storage = CreateInstance(folderManager: folderManager, project: project);

            await storage.CreateFolderAsync("Folder");

            Assert.False(result);
        }

        [Theory]
        [InlineData(@"C:\Project.csproj",           @"Properties\File.cs",                   @"C:\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties\File.cs",                   @"C:\Projects\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties\File.cs",                @"C:\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties\File.cs",                @"C:\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties\File.cs",                @"D:\Properties\File.cs")]
        [InlineData(@"C:\Project.csproj",           @"Properties\Folder\File.cs",            @"C:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties\Folder\File.cs",            @"C:\Projects\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties\Folder\File.cs",         @"C:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties\Folder\File.cs",         @"C:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties\Folder\File.cs",         @"D:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Project.csproj",           @"Folder With Spaces\File.cs",           @"C:\Folder With Spaces\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Folder With Spaces\Folder\File.cs",    @"C:\Projects\Folder With Spaces\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Folder With Spaces\Folder\File.cs", @"C:\Folder With Spaces\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Folder With Spaces\Folder\File.cs", @"C:\Folder With Spaces\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Folder With Spaces\Folder\File.cs", @"D:\Folder With Spaces\Folder\File.cs")]
        public async Task AddFileAsync_ValueAsPath_IsCalculatedRelativeToProjectDirectory(string projectPath, string input, string expected)
        {
            var project = UnconfiguredProjectFactory.Create(fullPath: projectPath);
            string? result = null;
            var sourceItemsProvider = IProjectItemProviderFactory.AddItemAsync(path => { result = path; return null!; });
            var storage = CreateInstance(sourceItemsProvider: sourceItemsProvider, project: project);

            await storage.AddFileAsync(input);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(@"C:\Project.csproj",           @"Properties\File.cs",                   @"C:\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties\File.cs",                   @"C:\Projects\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties\File.cs",                @"C:\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties\File.cs",                @"C:\Properties\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties\File.cs",                @"D:\Properties\File.cs")]
        [InlineData(@"C:\Project.csproj",           @"Properties\Folder\File.cs",            @"C:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Properties\Folder\File.cs",            @"C:\Projects\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Properties\Folder\File.cs",         @"C:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Properties\Folder\File.cs",         @"C:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Properties\Folder\File.cs",         @"D:\Properties\Folder\File.cs")]
        [InlineData(@"C:\Project.csproj",           @"Folder With Spaces\File.cs",           @"C:\Folder With Spaces\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"Folder With Spaces\Folder\File.cs",    @"C:\Projects\Folder With Spaces\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"..\Folder With Spaces\Folder\File.cs", @"C:\Folder With Spaces\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"C:\Folder With Spaces\Folder\File.cs", @"C:\Folder With Spaces\Folder\File.cs")]
        [InlineData(@"C:\Projects\Project.csproj",  @"D:\Folder With Spaces\Folder\File.cs", @"D:\Folder With Spaces\Folder\File.cs")]
        public async Task CreateEmptyFileAsync_ValueAsPath_IsCalculatedRelativeToProjectDirectory(string projectPath, string input, string expected)
        {
            var project = UnconfiguredProjectFactory.Create(fullPath: projectPath);
            string? result = null;
            var fileSystem = IFileSystemFactory.ImplementCreate(path => { result = path; });

            var storage = CreateInstance(fileSystem: fileSystem, project: project);

            await storage.CreateEmptyFileAsync(input);

            Assert.Equal(expected, result);
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
            var project = UnconfiguredProjectFactory.Create(fullPath: projectPath);
            string? result = null;
            var fileSystem = IFileSystemFactory.ImplementCreateDirectory(path => { result = path; });

            var storage = CreateInstance(fileSystem: fileSystem, project: project);

            await storage.CreateFolderAsync(input);

            Assert.Equal(expected, result);
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
        public async Task AddFolderAsync_ValueAsPath_IsCalculatedRelativeToProjectDirectory(string projectPath, string input, string expected)
        {
            var project = UnconfiguredProjectFactory.Create(fullPath: projectPath);
            string? result = null;
            var folderManager = IFolderManagerFactory.IncludeFolderInProjectAsync((path, _) => { result = path; });

            var storage = CreateInstance(folderManager: folderManager, project: project);

            await storage.AddFolderAsync(input);

            Assert.Equal(expected, result);
        }

        private static PhysicalProjectTreeStorage CreateInstance(IProjectTreeService? projectTreeService = null, IProjectItemProvider? sourceItemsProvider = null, IFileSystem? fileSystem = null, IFolderManager? folderManager = null, UnconfiguredProject? project = null)
        {
            projectTreeService ??= IProjectTreeServiceFactory.Create(ProjectTreeParser.Parse("Root"));
            fileSystem ??= IFileSystemFactory.Create();
            folderManager ??= IFolderManagerFactory.Create();
            sourceItemsProvider ??= IProjectItemProviderFactory.Create();
            project ??= UnconfiguredProjectFactory.Create();

            return new PhysicalProjectTreeStorage(
                project,
                projectTreeService,
                new Lazy<IFileSystem>(() => fileSystem),
                IActiveConfiguredValueFactory.ImplementValue(() => new PhysicalProjectTreeStorage.ConfiguredImports(folderManager, sourceItemsProvider)));
        }
    }
}
