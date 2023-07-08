// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Tree
{
    public class AppDesignerFolderProjectTreePropertiesProviderTests
    {
        [Fact]
        public void ProjectPropertiesRules_ReturnsAppDesigner()
        {
            var propertiesProvider = CreateInstance();

            Assert.Equal(propertiesProvider.ProjectPropertiesRules, new string[] { "AppDesigner" });
        }

        [Fact]
        public void UpdateProjectTreeSettings_NullAsRuleSnapshots_ThrowsArgumentNull()
        {
            var propertiesProvider = CreateInstance();
            IImmutableDictionary<string, string> projectTreeSettings = ImmutableStringDictionary<string>.EmptyOrdinal;

            Assert.Throws<ArgumentNullException>("ruleSnapshots", () =>
            {
                propertiesProvider.UpdateProjectTreeSettings(null!, ref projectTreeSettings);
            });
        }

        [Fact]
        public void UpdateProjectTreeSettings_NullAsProjectTreeSettings_ThrowsArgumentNull()
        {
            var ruleSnapshots = IProjectRuleSnapshotsFactory.Create();
            var propertiesProvider = CreateInstance();
            IImmutableDictionary<string, string>? projectTreeSettings = null;

            Assert.Throws<ArgumentNullException>("projectTreeSettings", () =>
            {
                propertiesProvider.UpdateProjectTreeSettings(ruleSnapshots, ref projectTreeSettings!);
            });
        }

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

        [Fact]
        public void ChangePropertyValues_TreeWithAppDesignerFolderButSupportsProjectDesignerFalse_ReturnsUnmodifiedTree()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);   // Don't support AppDesigner
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder})
                """);

            Verify(propertiesProvider, tree, tree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
            """)]
        public void ChangePropertyValues_TreeWithMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {   // "Properties" is the default, so we shouldn't find "My Project"
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            Verify(propertiesProvider, tree, tree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
                    AssemblyInfo.cs (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
                    AssemblyInfo.cs (flags: {})
                NotProperties (flags: {Folder})
            """)]
        public void ChangePropertyValues_TreeWithoutAppDesignerFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            Verify(propertiesProvider, tree, tree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {NotFolder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Unrecognized NotAFolder})
            """)]
        public void ChangePropertyValues_TreeWithFileCalledProperties_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            Verify(propertiesProvider, tree, tree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder IncludeInProjectCandidate})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {IncludeInProjectCandidate Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {IncludeInProjectCandidate})
            """)]
        public void ChangePropertyValues_TreeWithExcludedAppDesignerFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            Verify(propertiesProvider, tree, tree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
                    Properties (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder (flags: {Folder})
                    Folder (flags: {Folder})
                        Properties (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Folder1 (flags: {Folder})
                Folder2 (flags: {Folder})
                    Properties (flags: {Folder})
            """)]
        public void ChangePropertyValues_TreeWithNestedAppDesignerFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            Verify(propertiesProvider, tree, tree);
        }

        [Theory]
        [InlineData(
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder})
            """,
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder BubbleUp})
            """,
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder Unrecognized AppDesignerFolder})
            """,
            """
            Root(flags: {ProjectRoot})
                Properties (flags: {Folder Unrecognized AppDesignerFolder BubbleUp})
            """)]
        public void ChangePropertyValues_TreeWithAppDesignerFolderAlreadyMarkedAsAppDesignerOrBubbleup_AddsRemainingFlags(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            Verify(propertiesProvider, expectedTree, inputTree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder BubbleUp})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                properties (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                properties (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                PROPERTIES (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                PROPERTIES (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder UnrecognizedCapability})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder})
                    AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
                    AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder})
                    AssemblyInfo.cs (flags: {})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
                    AssemblyInfo.cs (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder})
                    Folder (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder (flags: {Folder})
            """)]
        public void ChangePropertyValues_TreeWithAppDesignerFolder_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            Verify(propertiesProvider, expectedTree, inputTree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {FileSystemEntity Folder})
                    Folder (flags: {FileSystemEntity Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {FileSystemEntity Folder AppDesignerFolder BubbleUp}), Icon: {259567C1-AA6B-46BF-811C-C145DD9F3B48 28}
                    Folder (flags: {FileSystemEntity Folder})
            """)]
        public void ChangePropertyValues_TreeWithAppDesignerFolder_SetsIconToAppDesignerFolder(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.AppDesignerFolder, new ProjectImageMoniker(new Guid("259567C1-AA6B-46BF-811C-C145DD9F3B48"), 28));
            var propertiesProvider = CreateInstance(imageProvider, designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            Verify(propertiesProvider, expectedTree, inputTree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {FileSystemEntity Folder})
                    Folder (flags: {FileSystemEntity Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {FileSystemEntity Folder AppDesignerFolder BubbleUp}), ExpandedIcon: {259567C1-AA6B-46BF-811C-C145DD9F3B48 29}
                    Folder (flags: {FileSystemEntity Folder})
            """)]
        public void ChangePropertyValues_TreeWithAppDesignerFolder_SetsExpandedIconToExpandedAppDesignerFolder(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ExpandedAppDesignerFolder, new ProjectImageMoniker(new Guid("259567C1-AA6B-46BF-811C-C145DD9F3B48"), 29));
            var propertiesProvider = CreateInstance(imageProvider, designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            Verify(propertiesProvider, expectedTree, inputTree);
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder}), Icon: {AE27A6B0-E345-4288-96DF-5EAF394EE369 1}, ExpandedIcon: {AE27A6B0-E345-4288-96DF-5EAF394EE369 2}
                    Folder (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp}), Icon: {AE27A6B0-E345-4288-96DF-5EAF394EE369 1}, ExpandedIcon: {AE27A6B0-E345-4288-96DF-5EAF394EE369 2}
                    Folder (flags: {Folder})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder}), Icon: {}, ExpandedIcon: {}
                    Folder (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                Properties (flags: {Folder AppDesignerFolder BubbleUp}), Icon: {}, ExpandedIcon: {}
                    Folder (flags: {Folder})
            """)]
        public void ChangePropertyValues_TreeWithAppDesignerFolderWhenImageProviderReturnsNull_DoesNotSetIconAndExpandedIcon(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.AppDesignerFolder, null);
            var propertiesProvider = CreateInstance(imageProvider, designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            Verify(propertiesProvider, expectedTree, inputTree);
        }

        [Fact]
        public void ChangePropertyValues_ProjectWithNoAppDesignerFolderProperty_DefaultsToProperties()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder})
                """);
            var expectedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder BubbleUp})
                """);

            Verify(propertiesProvider, expectedTree, inputTree, folderName: null);
        }

        [Fact]
        public void ChangePropertyValues_ProjectWithEmptyAppDesignerFolderProperty_DefaultsToProperties()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder})
                """);
            var expectedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder BubbleUp})
                """);

            Verify(propertiesProvider, expectedTree, inputTree, folderName: "");
        }

        [Fact]
        public void ChangePropertyValues_ProjectWithNonDefaultAppDesignerFolderProperty_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    FooBar (flags: {Folder})
                """);
            var expectedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    FooBar (flags: {Folder AppDesignerFolder BubbleUp})
                """);

            Verify(propertiesProvider, expectedTree, inputTree, folderName: "FooBar");
        }

        [Theory]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder BubbleUp})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                my project (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                my project (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                MY PROJECT (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                MY PROJECT (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder UnrecognizedCapability})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    My Project (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    My Project (flags: {Folder VisibleOnlyInShowAllFiles})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    AssemblyInfo.cs (flags: {IncludeInProjectCandidate})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    Folder (flags: {IncludeInProjectCandidate})
                        Item.cs (flags: {IncludeInProjectCandidate})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder (flags: {IncludeInProjectCandidate})
                        Item.cs (flags: {IncludeInProjectCandidate})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    Folder1 (flags: {IncludeInProjectCandidate})
                        Item.cs (flags: {IncludeInProjectCandidate})
                    Folder2 (flags: {Folder})
                        Item.cs (flags: {})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder1 (flags: {IncludeInProjectCandidate})
                        Item.cs (flags: {IncludeInProjectCandidate})
                    Folder2 (flags: {Folder VisibleOnlyInShowAllFiles})
                        Item.cs (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    Resources.resx (flags: {})
                        Resources.Designer.cs (flags: {})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    Resources.resx (flags: {VisibleOnlyInShowAllFiles})
                        Resources.Designer.cs (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    AssemblyInfo.cs (flags: {})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    AssemblyInfo.cs (flags: {VisibleOnlyInShowAllFiles})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    Folder (flags: {Folder})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder (flags: {Folder VisibleOnlyInShowAllFiles})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    Folder (flags: {Folder})
                        Folder (flags: {Folder})
                            File (flags: {})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder (flags: {Folder VisibleOnlyInShowAllFiles})
                        Folder (flags: {Folder})
                            File (flags: {})
            """)]
        [InlineData(
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder})
                    Folder1 (flags: {Folder})
                        Folder (flags: {Folder})
                            File (flags: {})
                    Folder2 (flags: {Folder})
                        Folder (flags: {Folder})
                            File (flags: {})
            """,
            """
            Root (flags: {ProjectRoot})
                My Project (flags: {Folder AppDesignerFolder BubbleUp})
                    Folder1 (flags: {Folder VisibleOnlyInShowAllFiles})
                        Folder (flags: {Folder})
                            File (flags: {})
                    Folder2 (flags: {Folder VisibleOnlyInShowAllFiles})
                        Folder (flags: {Folder})
                            File (flags: {})
            """)]
        public void ChangePropertyValues_TreeWithMyProjectCandidateAndContentVisibleOnlyInShowAllFiles_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp(string input, string expected)
        {   // Mimic's Visual Basic projects
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var propertiesProvider = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            Verify(propertiesProvider, expectedTree, inputTree, folderName: "My Project", contentOnlyVisibleInShowAllFiles: true);
        }

        internal static void Verify(AppDesignerFolderProjectTreePropertiesProvider provider, IProjectTree expected, IProjectTree input, string? folderName = null, bool? contentOnlyVisibleInShowAllFiles = null)
        {
            IImmutableDictionary<string, string> projectTreeSettings = ImmutableStringDictionary<string>.EmptyOrdinal;
            IImmutableDictionary<string, IProjectRuleSnapshot> ruleSnapshots = IProjectRuleSnapshotsFactory.Create();

            if (folderName is not null)
                ruleSnapshots = ruleSnapshots.Add(AppDesigner.SchemaName, AppDesigner.FolderNameProperty, folderName);

            if (contentOnlyVisibleInShowAllFiles != null)
                ruleSnapshots = ruleSnapshots.Add(AppDesigner.SchemaName, AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty, contentOnlyVisibleInShowAllFiles.Value.ToString());

            provider.UpdateProjectTreeSettings(ruleSnapshots, ref projectTreeSettings);

            IProjectTree result = provider.ChangePropertyValuesForEntireTree(input, projectTreeSettings);

            AssertAreEquivalent(expected, result);
        }

        private static void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            Assert.NotSame(expected, actual);

            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }

        private static AppDesignerFolderProjectTreePropertiesProvider CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static AppDesignerFolderProjectTreePropertiesProvider CreateInstance(IProjectDesignerService designerService)
        {
            return CreateInstance(null, designerService);
        }

        private static AppDesignerFolderProjectTreePropertiesProvider CreateInstance(IProjectImageProvider? imageProvider, IProjectDesignerService? designerService)
        {
            return new AppDesignerFolderProjectTreePropertiesProvider(
                imageProvider ?? IProjectImageProviderFactory.Create(),
                designerService ?? IProjectDesignerServiceFactory.Create());
        }
    }
}
