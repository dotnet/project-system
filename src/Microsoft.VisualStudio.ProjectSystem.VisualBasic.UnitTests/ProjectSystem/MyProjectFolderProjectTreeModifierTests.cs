// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Testing;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class MyProjectFolderProjectTreeModifierTests
    {
        [Fact]
        public void Constructor_NullAsImageProvider_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();
            var designerService = IProjectDesignerServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("imageProvider", () => {

                new MyProjectFolderProjectTreePropertiesProvider((IProjectImageProvider)null, projectServices, designerService);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectServices_ThrowsArgumentNull()
        {
            var imageProvider = IProjectImageProviderFactory.Create();
            var designerService = IProjectDesignerServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("projectServices", () => {

                new MyProjectFolderProjectTreePropertiesProvider(imageProvider, (IUnconfiguredProjectCommonServices)null, designerService);
            });
        }


        [Fact]
        public void Constructor_NullAsDesignerService_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();
            var imageProvider = IProjectImageProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("designerService", () => {

                new MyProjectFolderProjectTreePropertiesProvider(imageProvider, projectServices, (IProjectDesignerService)null);
            });
        }

        [Fact]
        public void ApplyModifications1_NullAsTree_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var projectTreeProvider = IProjectTreeProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("tree", () => {

                modifier.ApplyModifications((IProjectTree)null, projectTreeProvider);
            });
        }

        [Fact]
        public void ApplyModifications2_NullAsTree_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var projectTreeProvider = IProjectTreeProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("tree", () => {

                modifier.ApplyModifications((IProjectTree)null, (IProjectTree)null, projectTreeProvider);
            });
        }

        [Fact]
        public void ApplyModifications1_NullAsTreeProvider_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var tree = ProjectTreeParser.Parse("Root");

            Assert.Throws<ArgumentNullException>("projectTreeProvider", () => {

                modifier.ApplyModifications(tree, (IProjectTreeProvider)null);
            });
        }

        [Fact]
        public void ApplyModifications2_NullAsTreeProvider_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var tree = ProjectTreeParser.Parse("Root");

            Assert.Throws<ArgumentNullException>("projectTreeProvider", () => {

                modifier.ApplyModifications(tree, (IProjectTree)null, (IProjectTreeProvider)null);
            });
        }

        [Fact]
        public void ApplyModifications_TreeWithMyProjectCandidateButSupportsProjectDesignerFalse_ReturnsUnmodifiedTree()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);   // Don't support AppDesigner
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
")]
        public void ApplyModifications_TreeWithPropertiesFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
        AssemblyInfo.cs (flags: {})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
        AssemblyInfo.cs (flags: {})
    NotMy Project (flags: {Folder})
")]
        public void ApplyModifications_TreeWithoutMyProjectCandidate_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {NotFolder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Unrecognized NotAFolder})
")]
        public void ApplyModifications_TreeWithFileCalledMyProject_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {IncludeInProjectCandidate Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {IncludeInProjectCandidate})
")]        
        public void ApplyModifications_TreeWithExcludedMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
        My Project (flags: {Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
        Folder (flags: {Folder})
            My Project (flags: {Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder1 (flags: {Folder})
    Folder2 (flags: {Folder})
        My Project (flags: {Folder})
")]        
        public void ApplyModifications_TreeWithNestedMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }
        
        [Theory]
        [InlineData(@"
Root(flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root(flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder})
")]
        [InlineData(@"
Root(flags: {ProjectRoot})
    My Project (flags: {Folder Unrecognized AppDesignerFolder})
")]
        public void ApplyModifications_TreeWithMyProjectCandidateAlreadyMarkedAsAppDesigner_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);
            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder BubbleUp})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    my project (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    my project (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    MY PROJECT (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    MY PROJECT (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder UnrecognizedCapability})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate VisibleOnlyInShowAllFiles})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
        AssemblyInfo.cs (flags: {})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
        Folder (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
        Folder (flags: {Folder VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
        Folder (flags: {Folder})
            Folder (flags: {Folder})
                File (flags: {})
", @"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
        Folder (flags: {Folder VisibleOnlyInShowAllFiles})
            Folder (flags: {Folder VisibleOnlyInShowAllFiles})
                File (flags: {VisibleOnlyInShowAllFiles})
")]
        public void ApplyModifications_TreeWithMyProjectCandidate_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithNullMyProjectFolder_DefaultsToMyProject()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService, appDesignerFolder: null);

            var inputTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithEmptyMyProjectFolder_DefaultsToMyProject()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService, appDesignerFolder: "");

            var inputTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithNonDefaultMyProjectFolder_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService, appDesignerFolder: "FooBar");

            var inputTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    FooBar (flags: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    FooBar (flags: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        private void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }

        private MyProjectFolderProjectTreePropertiesProvider CreateInstance()
        {
            return CreateInstance((IProjectImageProvider)null, (IProjectDesignerService)null);
        }

        private MyProjectFolderProjectTreePropertiesProvider CreateInstance(IProjectDesignerService designerService, string appDesignerFolder = "My Project")
        {
            return CreateInstance((IProjectImageProvider)null, designerService, appDesignerFolder);
        }

        private MyProjectFolderProjectTreePropertiesProvider CreateInstance(IProjectImageProvider imageProvider, IProjectDesignerService designerService, string appDesignerFolder = "My Project")
        {
            designerService = designerService ?? IProjectDesignerServiceFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject, 
                new PropertyPageData() {
                    Category = nameof(ConfigurationGeneral),
                    PropertyName = nameof(ConfigurationGeneral.AppDesignerFolder),
                    Value = appDesignerFolder
                });

            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create(threadingService, projectProperties.ConfiguredProject, projectProperties);

            return new MyProjectFolderProjectTreePropertiesProvider(imageProvider ?? IProjectImageProviderFactory.Create(), projectServices, designerService);
        }
    }
}
