// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Tree
{
    public class ProjectRootImageProjectTreeModifierTests
    {
        [Fact]
        public void CalculatePropertyValues_NullAsPropertyContext_ThrowsArgumentNull()
        {
            var propertyValues = IProjectTreeCustomizablePropertyValuesFactory.Create();
            var propertiesProvider = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyContext", () =>
            {
                propertiesProvider.CalculatePropertyValues(null!, propertyValues);
            });
        }

        [Fact]
        public void CalculatePropertyValues_NullAsPropertyValues_ThrowsArgumentNull()
        {
            var propertyContext = IProjectTreeCustomizablePropertyContextFactory.Create();
            var propertiesProvider = CreateInstance();

            Assert.Throws<ArgumentNullException>("propertyValues", () =>
            {
                propertiesProvider.CalculatePropertyValues(propertyContext, null!);
            });
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {Unrecognized ProjectRoot})
            """,
            """
            Root (flags: {Unrecognized ProjectRoot}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
            """)]
        [InlineData(
            """
            Root (flags: {Unrecognized ProjectRoot})
                Folder (flags: {Folder})
            """,
            """
            Root (flags: {Unrecognized ProjectRoot}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
                Folder (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
                Folder (flags: {Folder})
            """)]
        public void CalculatePropertyValues_ProjectRootAsTree_SetsIconToProjectRoot(string input, string expected)
        {
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var propertiesProvider = CreateInstance(imageProvider);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {Unrecognized ProjectRoot})
            """,
            """
            Root (flags: {Unrecognized ProjectRoot}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
            """)]
        [InlineData(
            """
            Root (flags: {Unrecognized ProjectRoot})
                Folder (flags: {Folder})
            """,
            """
            Root (flags: {Unrecognized ProjectRoot}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
                Folder (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
                Folder (flags: {Folder})
            """)]
        public void CalculatePropertyValues_WhenSharedProjectRootAsTree_SetsIconToSharedProjectRoot(string input, string expected)
        {
            var capabilities = IProjectCapabilitiesServiceFactory.ImplementsContains(capability =>
            {
                return capability == ProjectCapabilities.SharedAssetsProject;
            });

            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.SharedProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var propertiesProvider = CreateInstance(capabilities, imageProvider);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Shared.items (flags: {SharedItemsImportFile})
            """,
            """
            Root (flags: {ProjectRoot})
                Shared.items (flags: {SharedItemsImportFile}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Shared.items (flags: {SharedItemsImportFile Unrecognized})
            """,
            """
            Root (flags: {ProjectRoot})
                Shared.items (flags: {SharedItemsImportFile Unrecognized}), Icon: {A140CD9F-FF94-483C-87B1-9EF5BE9F469A 1}, ExpandedIcon: {}
            """)]
        public void CalculatePropertyValues_WhenSharedItemsImportFileAsTree_SetsIconToSharedItemsImportFile(string input, string expected)
        {
            var capabilities = IProjectCapabilitiesServiceFactory.ImplementsContains(capability =>
            {
                return capability == ProjectCapabilities.SharedAssetsProject;
            });

            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.SharedItemsImportFile, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var propertiesProvider = CreateInstance(capabilities, imageProvider);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(inputTree);

            AssertAreEquivalent(expectedTree, result);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                File (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                File (flags: {IncludeInProjectCandidate})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder IncludeInProjectCandidate})
            """)]
        public void CalculatePropertyValues_NonProjectRootAsTree_DoesNotSetIcon(string input)
        {
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var propertiesProvider = CreateInstance(imageProvider);

            var tree = ProjectTreeParser.Parse(input);
            var result = propertiesProvider.ChangePropertyValuesForEntireTree(tree.Children[0]);

            Assert.Null(result.Icon);
        }

        [Fact]
        public void CalculatePropertyValues_ProjectRootAsTreeWhenImageProviderReturnsNull_DoesNotSetIcon()
        {
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage((string key) => null);

            var propertiesProvider = CreateInstance(imageProvider);

            var icon = new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1);
            var tree = ProjectTreeParser.Parse("Root (flags: {ProjectRoot})");

            tree = tree.SetIcon(icon);

            propertiesProvider.ChangePropertyValuesForEntireTree(tree);

            Assert.Same(icon, tree.Icon);
        }

        private static void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            Assert.NotSame(expected, actual);

            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }

        private static ProjectRootImageProjectTreePropertiesProvider CreateInstance()
        {
            return CreateInstance(null!);
        }

        private static ProjectRootImageProjectTreePropertiesProvider CreateInstance(IProjectImageProvider imageProvider)
        {
            return CreateInstance(null!, imageProvider);
        }

        private static ProjectRootImageProjectTreePropertiesProvider CreateInstance(IProjectCapabilitiesService? capabilities, IProjectImageProvider? imageProvider)
        {
            capabilities ??= IProjectCapabilitiesServiceFactory.Create();
            imageProvider ??= IProjectImageProviderFactory.Create();

            return new ProjectRootImageProjectTreePropertiesProvider(capabilities, imageProvider);
        }
    }
}
