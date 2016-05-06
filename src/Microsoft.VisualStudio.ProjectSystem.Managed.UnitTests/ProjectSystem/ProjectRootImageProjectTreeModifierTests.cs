// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class ProjectRootImageProjectTreeModifierTests
    {
        [Fact]
        public void Constructor_NullAsImageProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("imageProvider", () => {
                new ProjectRootImageProjectTreePropertiesProvider((IProjectImageProvider)null);
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

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
")]
        [InlineData(@"
Root (flags: {ProjectRoot Unrecognized})
")]
        [InlineData(@"
Root (flags: {Unrecognized ProjectRoot})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
")]
        public void CalculatePropertyValues_ProjectRootAsTree_SetsIconToProjectRoot(string input)
        {
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var propertiesProvider = CreateInstance(imageProvider);

            var tree = ProjectTreeParser.Parse(input);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            Assert.Equal(new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1), tree.Icon);
        }

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot})
    File (flags: {})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    File (flags: {IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder})
")]
        [InlineData(@"
Root (flags: {ProjectRoot})
    Folder (flags: {Folder IncludeInProjectCandidate})
")]
        public void CalculatePropertyValues_NonProjectRootAsTree_DoesNotSetIcon(string input)
        {
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var propertiesProvider = CreateInstance(imageProvider);

            var tree = ProjectTreeParser.Parse(input);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree.Children[0]);

            Assert.Null(tree.Icon);
        }

        [Fact]
        public void CalculatePropertyValues_ProjectRootAsTreeWhenImageProviderReturnsNull_DoesNotSetIcon()
        {
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage((string key) => null);

            var propertiesProvider = CreateInstance(imageProvider);

            var icon = new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1);
            var tree = ProjectTreeParser.Parse("Root (flags: {ProjectRoot})");

            tree = tree.SetIcon(icon);

            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            Assert.Same(icon, tree.Icon);
        }

        private ProjectRootImageProjectTreePropertiesProvider CreateInstance()
        {
            return CreateInstance((IProjectImageProvider)null);
        }

        private ProjectRootImageProjectTreePropertiesProvider CreateInstance(IProjectImageProvider imageProvider)
        {
            imageProvider = imageProvider ?? IProjectImageProviderFactory.Create();

            return new ProjectRootImageProjectTreePropertiesProvider(imageProvider);
        }
    }
}
