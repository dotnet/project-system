// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders;
using Xunit;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class SpecialFileProviderTests
    {
        [Fact]
        public void Constructor_NullProjectTreeService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("projectTreeService", () =>
            {
                new SettingsFileSpecialFileProvider(null, IProjectItemProviderFactory.Create(), null, IFileSystemFactory.Create(null));
            });
        }

        [Fact]
        public void Constructor_NullSourceItemsProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("sourceItemsProvider", () =>
            {
                new SettingsFileSpecialFileProvider(IProjectTreeServiceFactory.Create(null), null, null, IFileSystemFactory.Create(null));
            });
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("fileSystem", () =>
            {
                new SettingsFileSpecialFileProvider(IProjectTreeServiceFactory.Create(null), IProjectItemProviderFactory.Create(), null, null);
            });
        }

        [Theory]
        // No file - return default path.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
", @"C:\Foo\Settings.settings")]
        // File in app designer folder
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", @"C:\Foo\Properties\Settings.settings")]
        // File in root folder.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
    Settings.settings, FilePath: ""C:\Foo\Settings.settings""
", @"C:\Foo\Settings.settings")]
        // Linked file inside app designer folder.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings (flags: {Linked}), FilePath: ""C:\SomeOtherPath\Settings.settings""
", @"C:\SomeOtherPath\Settings.settings")]
        // File inside a non-app designer folder - return default path.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", @"C:\Foo\Settings.settings")]
        public async Task FindFile_FromAppDesignerFolder(string input, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.Create();
            var fileSystem = IFileSystemFactory.Create(path => true);

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);

            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.FullPath);
            Assert.Equal(expectedFilePath, filePath);
        }

        [Theory]
        // File exists in the root folder.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
    App.config, FilePath: ""C:\Foo\App.config""
", @"C:\Foo\App.config")]
        // File exists in the app designer folder - should return default path under root.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        App.config, FilePath: ""C:\Foo\Properties\App.config""
", @"C:\Foo\App.config")]
        public async Task FindFile_FromRoot(string input, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.Create();
            var fileSystem = IFileSystemFactory.Create(path => true);

            var provider = new AppConfigFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);

            var filePath = await provider.GetFileAsync(SpecialFiles.AppConfig, SpecialFileFlags.FullPath);
            Assert.Equal(expectedFilePath, filePath);
        }

        [Theory]
        // A folder with the right name exists at the right place - should return default path under root.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings (flags: {Folder}), FilePath: ""C:\Foo\Properties\Settings.settings""
", @"C:\Foo\Settings.settings")]
        public async Task FindFile_IgnoreFolder(string input, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.Create();
            var fileSystem = IFileSystemFactory.Create(path => true);

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);

            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.FullPath);
            Assert.Equal(expectedFilePath, filePath);
        }

        [Theory]
        // A file exists in the tree but not in the file system
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", /*fileExistsOnDisk*/ false, @"C:\Foo\Settings.settings")]
        // A file exists on disk but is not included in the project.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings (flags: {IncludeInProjectCandidate}), FilePath: ""C:\Foo\Properties\Settings.settings""
", /*fileExistsOnDisk*/ true, @"C:\Foo\Settings.settings")]
        public async Task FindFile_NodeNotInSync(string input, bool fileExistsOnDisk, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.Create();
            var fileSystem = IFileSystemFactory.Create(path => fileExistsOnDisk);

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);

            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.FullPath);
            Assert.Equal(expectedFilePath, filePath);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", @"C:\Foo\Properties\Settings.settings")]
        public async Task CreateFile_InAppDesignerFolder(string input, string expected, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.CreateWithAdd(inputTree);
            var fileSystem = IFileSystemFactory.Create(path => false,
                                                       path =>
                                                       {
                                                           // Verify that file is created on disk.
                                                           Assert.Equal(expectedFilePath, path);
                                                           return null;
                                                       });

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);
            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(expectedFilePath, filePath);
            AssertAreEquivalent(expectedTree, inputTree);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
    App.config, FilePath: ""C:\Foo\App.config""
", @"C:\Foo\App.config")]
        public async Task CreateFile_InRootFolder(string input, string expected, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.CreateWithAdd(inputTree);
            var fileSystem = IFileSystemFactory.Create(path => false,
                                                       path =>
                                                       {
                                                           // Verify that file is created on disk.
                                                           Assert.Equal(expectedFilePath, path);
                                                           return null;
                                                       });

            var provider = new AppConfigFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);
            var filePath = await provider.GetFileAsync(SpecialFiles.AppConfig, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(expectedFilePath, filePath);
            AssertAreEquivalent(expectedTree, inputTree);
        }

        [Theory]
        // A file exists in the tree but not in the file system
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", /*fileExistsOnDisk*/ false, @"C:\Foo\Properties\Settings.settings")]
        // A file exists on disk but is not included in the project.
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings (flags: {IncludeInProjectCandidate}), FilePath: ""C:\Foo\Properties\Settings.settings""
", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", /*fileExistsOnDisk*/ true, @"C:\Foo\Properties\Settings.settings")]
        public async Task CreateFile_NotInSync(string input, string expected, bool fileExistsOnDisk, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.CreateWithAdd(inputTree);
            var fileSystem = IFileSystemFactory.Create(path => fileExistsOnDisk, 
                                                       path => 
                                                       {
                                                           // Verify that file is created on disk.
                                                           Assert.False(fileExistsOnDisk);
                                                           Assert.Equal(expectedFilePath, path);
                                                           return null;
                                                       });

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);
            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(expectedFilePath, filePath);
            AssertAreEquivalent(expectedTree, inputTree);
        }


        [Theory]
        // A file exists in the tree but not in the file system
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.csproj""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", @"C:\Foo\Properties\Settings.settings")]
        public async Task CreateFile_CallTemplateProvider(string input, string expected, string expectedFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.CreateWithAdd(inputTree);
            var fileSystem = IFileSystemFactory.Create(path => false,
                                                       path =>
                                                       {
                                                           // Verify that file is created on disk.
                                                           Assert.False(true, "File system create shouldn't have been called directly.");
                                                           return null;
                                                       });
            var templateProvider = new Lazy<ICreateFileFromTemplateService>(() => ICreateFileFromTemplateServiceFactory.Create());

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, templateProvider, fileSystem);
            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.CreateIfNotExist);

            Assert.Equal(expectedFilePath, filePath);
        }

        private void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            Assert.NotSame(expected, actual);

            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }
    }
}
