// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq.Protected;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    public class WindowsFormsEditorProviderTests
    {
        [Fact]
        public async Task GetSpecificEditorAsync_NullAsDocumentMoniker_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("documentMoniker", () =>
            {
                return provider.GetSpecificEditorAsync(null!);
            });
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_NullAsDocumentMoniker_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("documentMoniker", () =>
            {
                return provider.SetUseGlobalEditorAsync(null!, false);
            });
        }

        [Fact]
        public async Task GetSpecificEditorAsync_EmptyAsDocumentMoniker_ThrowsArgument()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("documentMoniker", () =>
            {
                return provider.GetSpecificEditorAsync(string.Empty);
            });
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_EmptyAsDocumentMoniker_ThrowsArgument()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("documentMoniker", () =>
            {
                return provider.SetUseGlobalEditorAsync(string.Empty, false);
            });
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenNoProjectSpecificEditorProviders_ReturnsNull()
        {
            var provider = CreateInstance();

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenNoDefaultProjectSpecificEditorProviders_ReturnsNull()
        {
            var editorProvider = IProjectSpecificEditorProviderFactory.ImplementGetSpecificEditorAsync();
            var provider = CreateInstance();

            provider.ProjectSpecificEditorProviders.Add("NotDefault", editorProvider);

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenFileNotInProject_ReturnsNull()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                """);
            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_WhenFileNotInProject_ReturnsFalse()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                """);
            var result = await provider.SetUseGlobalEditorAsync(@"C:\Foo.cs", true);

            Assert.False(result);
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenFileNotCompileItem_ReturnsNull()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs, FilePath: "C:\Foo.cs", ItemType: None
                """);

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_WhenFileNotCompileItem_ReturnsFalse()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs, FilePath: "C:\Foo.cs", ItemType: None
                """);

            var result = await provider.SetUseGlobalEditorAsync(@"C:\Foo.cs", false);

            Assert.False(result);
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenCompileItemWithNoSubType_ReturnsNull()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs, FilePath: "C:\Foo.cs", ItemType: Compile
                """);

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_WhenCompileItemWithNoSubType_ReturnsFalse()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs, FilePath: "C:\Foo.cs", ItemType: Compile
                """);

            var result = await provider.SetUseGlobalEditorAsync(@"C:\Foo.cs", false);

            Assert.False(result);
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenCompileItemWithUnrecognizedSubType_ReturnsNull()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Code
                """);

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_WhenCompileItemWithUnrecognizedSubType_ReturnsFalse()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Code
                """);

            var result = await provider.SetUseGlobalEditorAsync(@"C:\Foo.cs", false);

            Assert.False(result);
        }

        [Fact]
        public async Task GetSpecificEditorAsync_WhenParentIsSourceFile_ReturnsNull()
        {   // Let's folks double-click the designer file to open it as text
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs (flags: {SourceFile})
                        Foo.Designer.cs, FilePath: "C:\Foo.Designer.cs", ItemType: Compile, SubType: Designer

                """);

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.Designer.cs");

            Assert.Null(result);
        }

        [Fact]
        public async Task SetUseGlobalEditorAsync_WhenParentIsSourceFile_ReturnsFalse()
        {
            var provider = CreateInstanceWithDefaultEditorProvider(
                """
                Project
                    Foo.cs (flags: {SourceFile})
                        Foo.Designer.cs, FilePath: "C:\Foo.Designer.cs", ItemType: Compile, SubType: Designer

                """);

            var result = await provider.SetUseGlobalEditorAsync(@"C:\Foo.Designer.cs", false);

            Assert.False(result);
        }

        [Theory]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Form
            """, true)]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Designer
            """, true)]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: UserControl
            """, true)]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Component
            """, false)]
        public async Task GetSpecificEditorAsync_WhenMarkedWithRecognizedSubType_ReturnsResult(string tree, bool useDesignerByDefault)
        {
            var defaultEditorFactory = Guid.NewGuid();

            var options = IProjectSystemOptionsFactory.ImplementGetUseDesignerByDefaultAsync((_, defaultValue, _) => defaultValue);
            var provider = CreateInstanceWithDefaultEditorProvider(tree, options, defaultEditorFactory);

            var result = await provider.GetSpecificEditorAsync(@"C:\Foo.cs");

            Assert.NotNull(result);
            Assert.NotEmpty(result.DisplayName);
            Assert.Equal(VSConstants.LOGVIEWID.Designer_guid, result.DefaultView);
            Assert.Equal(useDesignerByDefault, result.IsDefaultEditor);
            Assert.Equal(defaultEditorFactory, result.EditorFactory);
        }

        [Theory]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Form
            """,
            "Form")]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Designer
            """,
            "Designer")]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: UserControl
            """,
            "UserControl")]
        [InlineData(
            """
            Project
                Foo.txt, FilePath: "C:\Foo.cs", ItemType: Compile, SubType: Component
            """,
            "Component")]
        public async Task SetUseGlobalEditorAsync_WhenMarkedWithRecognizedSubType_ReturnsTrue(string tree, string expectedCategory)
        {
            string? categoryResult = null;
            bool? valueResult = null;
            var options = IProjectSystemOptionsFactory.ImplementSetUseDesignerByDefaultAsync((category, value, _) => { categoryResult = category; valueResult = value; return Task.CompletedTask; });
            var provider = CreateInstanceWithDefaultEditorProvider(tree, options);

            var result = await provider.SetUseGlobalEditorAsync(@"C:\Foo.cs", useGlobalEditor: true);

            Assert.True(result);
            Assert.False(valueResult);
            Assert.Equal(expectedCategory, categoryResult);
        }

        private static WindowsFormsEditorProvider CreateInstanceWithDefaultEditorProvider(string projectTree, IProjectSystemOptions? options = null, Guid defaultEditorFactory = default)
        {
            var tree = ProjectTreeParser.Parse(projectTree);

            var defaultEditorProvider = IProjectSpecificEditorProviderFactory.ImplementGetSpecificEditorAsync(defaultEditorFactory);

            var provider = CreateInstance(projectTree: IPhysicalProjectTreeFactory.Create(currentTree: tree), options: options);
            provider.ProjectSpecificEditorProviders.Add("Default", defaultEditorProvider);

            return provider;
        }

        private static WindowsFormsEditorProvider CreateInstance(UnconfiguredProject? unconfiguredProject = null, IPhysicalProjectTree? projectTree = null, IProjectSystemOptions? options = null)
        {
            var project = ConfiguredProjectFactory.Create();
            unconfiguredProject ??= UnconfiguredProjectFactory.Create(configuredProject: project);
            projectTree ??= IPhysicalProjectTreeFactory.Create();
            options ??= IProjectSystemOptionsFactory.Create();

            var provider = new Mock<WindowsFormsEditorProvider>(unconfiguredProject, projectTree.AsLazy(), options.AsLazy());
            provider.Protected().Setup<IRule?>("GetBrowseObjectProperties", ItExpr.IsAny<ConfiguredProject>(), ItExpr.IsAny<IProjectItemTree>())
                    .Returns((ConfiguredProject configuredProject, IProjectItemTree node) => node.BrowseObjectProperties);

            return provider.Object;
        }
    }
}
