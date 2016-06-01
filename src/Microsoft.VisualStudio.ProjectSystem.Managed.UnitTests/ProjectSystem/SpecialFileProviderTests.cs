// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class SpecialFileProviderTests
    {
        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
", @"C:\Foo\Settings.settings")]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""
    Properties (flags: {Folder AppDesignerFolder}), FilePath: ""C:\Foo\Properties""
        Settings.settings, FilePath: ""C:\Foo\Properties\Settings.settings""
", @"C:\Foo\Properties\Settings.settings")]
        public async Task FindsTheFileOrReturnsDefaultPath(string input, string specialFilePath)
        {
            var inputTree = ProjectTreeParser.Parse(input);

            var projectTreeService = IProjectTreeServiceFactory.Create(inputTree);
            var sourceItemsProvider = IProjectItemProviderFactory.Create();
            var fileSystem = IFileSystemFactory.CreateWithExists(path => input.Contains(path));

            var provider = new SettingsFileSpecialFileProvider(projectTreeService, sourceItemsProvider, null, fileSystem);

            var filePath = await provider.GetFileAsync(SpecialFiles.AppSettings, SpecialFileFlags.FullPath);
            Assert.Equal(specialFilePath, filePath);
        }
    }
}
