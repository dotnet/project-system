// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [Trait("UnitTest", "ProjectSystem")]
    public class AppDesignerFolderSpecialFileProviderTests
    {
        [Theory]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
",
@"C:\Project\Properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    properties (flags: {Folder AppDesignerFolder BubbleUp})
",
@"C:\Project\properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    PROPERTIES (flags: {Folder AppDesignerFolder BubbleUp})
",
@"C:\Project\PROPERTIES")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Properties (flags: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
",
@"C:\Project\Properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
",
@"C:\Project\Properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {})
",
@"C:\Project\Properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
        Folder (flags: {Folder})
",
@"C:\Project\Properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Folder (flags: {Folder})
        Properties (flags: {Folder AppDesignerFolder BubbleUp})
",
@"C:\Project\Folder\Properties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Folder (flags: {Folder})
        NotCalledProperties (flags: {Folder AppDesignerFolder BubbleUp})
",
@"C:\Project\Folder\NotCalledProperties")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Folder (flags: {Folder})
        My Project (flags: {Folder AppDesignerFolder BubbleUp})
",
@"C:\Project\Folder\My Project")]
        [InlineData(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Folder1 (flags: {Folder})
    Folder2 (flags: {Folder})
        My Project (flags: {Folder AppDesignerFolder BubbleUp})
",
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

        [Theory]    // AppDesignerFolder        // Expected return
        [InlineData(@"Properties",              @"C:\Project\Properties")]
        [InlineData(@"My Project",              @"C:\Project\My Project")]
        [InlineData(@"Folder\AppDesigner",      @"C:\Project\Folder\AppDesigner")]
        [InlineData(@"",                        null)]
        public async Task GetFile_WhenTreeWithoutAppDesignerFolder_ReturnsDefaultAppDesignerFolder(string input, string expected)
        {
            var tree = ProjectTreeParser.Parse(@"
Project (flags: {ProjectRoot}), FilePath: ""C:\Project\Project.csproj""
    Properties (flags: {Folder})
");

            var treeProvider = new ProjectTreeProvider();
            var physicalProjectTree = IPhysicalProjectTreeFactory.Create(provider: treeProvider, currentTree: tree);

            var projectProperties = CreateProperties(appDesignerFolderName: input);

            var provider = CreateInstance(physicalProjectTree, projectProperties);

            var result = await provider.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.FullPath);

            Assert.Equal(expected, result);
        }

        private ProjectProperties CreateProperties(string appDesignerFolderName)
        {
            return ProjectPropertiesFactory.Create(UnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = AppDesigner.SchemaName, PropertyName = AppDesigner.FolderNameProperty, Value = appDesignerFolderName }
                });
        }

        private AppDesignerFolderSpecialFileProvider CreateInstance(IPhysicalProjectTree physicalProjectTree = null, ProjectProperties properties = null)
        {
            physicalProjectTree = physicalProjectTree ?? IPhysicalProjectTreeFactory.Create();
            properties = properties ?? ProjectPropertiesFactory.CreateEmpty();

            return new AppDesignerFolderSpecialFileProvider(new Lazy<IPhysicalProjectTree>(() => physicalProjectTree), properties);
        }
    }
}
