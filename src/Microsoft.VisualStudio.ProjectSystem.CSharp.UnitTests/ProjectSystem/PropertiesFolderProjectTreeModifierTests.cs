// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Testing;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class PropertiesFolderProjectTreeModifierTests
    {
        [Fact]
        public void Constructor_NullAsImageProvider_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();
            var designerService = IProjectDesignerServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("imageProvider", () => {

                new PropertiesFolderProjectTreePropertiesProvider((IProjectImageProvider)null, projectServices, designerService);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectServices_ThrowsArgumentNull()
        {
            var imageProvider = IProjectImageProviderFactory.Create();
            var designerService = IProjectDesignerServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("projectServices", () => {

                new PropertiesFolderProjectTreePropertiesProvider(imageProvider, (IUnconfiguredProjectCommonServices)null, designerService);
            });
        }

        [Fact]
        public void Constructor_NullAsDesignerService_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();
            var imageProvider = IProjectImageProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("designerService", () => {

                new PropertiesFolderProjectTreePropertiesProvider(imageProvider, projectServices, (IProjectDesignerService)null);
            });
        }

        [Fact]
        public void CalculatePropertyValues_NullAsPropertyContext_ThrowsArgumentNull()
        {
            var propertyValues = IProjectTreeCustomizablePropertyValuesFactory.Create();
            var propertiesProvider = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyContext", () => {
                propertiesProvider.CalculatePropertyValues((IProjectTreeCustomizablePropertyContext)null, propertyValues);
            });
        }

        [Fact]
        public void CalculatePropertyValues_NullAsPropertyValues_ThrowsArgumentNull()
        {
            var propertyContext = IProjectTreeCustomizablePropertyContextFactory.Create();
            var propertiesProvider = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyValues", () => {
                propertiesProvider.CalculatePropertyValues(propertyContext, (IProjectTreeCustomizablePropertyValues)null);
            });
        }

        [Fact]
        public void ChangePropertyValues_TreeWithPropertiesCandidateButSupportsProjectDesignerFalse_ReturnsUnmodifiedTree()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);   // Don't support AppDesigner
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
");

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    My Project (flags: {Folder})
")]
        public void ChangePropertyValues_TreeWithMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

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
    NotProperties (flags: {Folder})
")]
        public void ChangePropertyValues_TreeWithoutPropertiesCandidate_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {NotFolder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Unrecognized NotAFolder})
")]
        public void ChangePropertyValues_TreeWithFileCalledProperties_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {IncludeInProjectCandidate Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {IncludeInProjectCandidate})
")]        
        public void ChangePropertyValues_TreeWithExcludedPropertiesFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
        Properties (flags: {Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
        Folder (flags: {Folder})
            Properties (flags: {Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder1 (flags: {Folder})
    Folder2 (flags: {Folder})
        Properties (flags: {Folder})
")]        
        public void ChangePropertyValues_TreeWithNestedPropertiesFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            AssertAreEquivalent(tree, result);
        }
        
        [Theory]
        [InlineData(@"
Root(flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root(flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder})
")]
        [InlineData(@"
Root(flags: {ProjectRoot})
    Properties (flags: {Folder Unrecognized AppDesignerFolder})
")]
        public void ChangePropertyValues_TreeWithPropertiesCandidateAlreadyMarkedAsAppDesigner_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder BubbleUp})
", @"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    properties (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    properties (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    PROPERTIES (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    PROPERTIES (flags: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder UnrecognizedCapability})
", @"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
", @"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
        AssemblyInfo.cs (flags: {})
", @"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (flags: {})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
        Folder (flags: {Folder})
", @"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
        Folder (flags: {Folder})
")]
        public void ChangePropertyValues_TreeWithPropertiesCandidate_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ChangePropertyValues_ProjectWithNullPropertiesFolder_DefaultsToProperties()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService, appDesignerFolder: null);

            var inputTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
");

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ChangePropertyValues_ProjectWithEmptyPropertiesFolder_DefaultsToProperties()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService, appDesignerFolder: "");

            var inputTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder BubbleUp})
");

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ChangePropertyValues_ProjectWithNonDefaultPropertiesFolder_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService, appDesignerFolder: "FooBar");

            var inputTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    FooBar (flags: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    FooBar (flags: {Folder AppDesignerFolder BubbleUp})
");

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        private void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }

        private PropertiesFolderProjectTreePropertiesProvider CreateInstance()
        {
            return CreateInstance((IProjectImageProvider)null, (IProjectDesignerService)null);
        }

        private PropertiesFolderProjectTreePropertiesProvider CreateInstance(IProjectDesignerService designerService, string appDesignerFolder = "Properties")
        {
            return CreateInstance((IProjectImageProvider)null, designerService, appDesignerFolder);
        }

        private PropertiesFolderProjectTreePropertiesProvider CreateInstance(IProjectImageProvider imageProvider, IProjectDesignerService designerService, string appDesignerFolder = "Properties")
        {
            designerService = designerService ?? IProjectDesignerServiceFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject,
                new PropertyPageData() {
                    Category = nameof(ConfigurationGeneral),
                    PropertyName = nameof(ConfigurationGeneral.AppDesignerFolder),
                    Value = appDesignerFolder,
                });

            var services = IUnconfiguredProjectCommonServicesFactory.Create(threadingService, projectProperties.ConfiguredProject, projectProperties);

            return new PropertiesFolderProjectTreePropertiesProvider(imageProvider ?? IProjectImageProviderFactory.Create(), services, designerService);
        }
    }
}
