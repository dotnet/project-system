// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    public class OrderingHelperTests
    {
        private static void AssertEqualProject(string expected, Project project)
        {
            using var writer = new StringWriter();

            project.Save(writer);

            Assert.Equal(expected, writer.ToString());
        }

        private static (string tempPath, string testPropsFile) CreateTempPropsFilePath()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var testPropsFile = Path.Combine(tempPath, "test.props");
            return (tempPath, testPropsFile);
        }

        private static Project CreateProjectWithImport(ProjectRootElement projectRootElement, ProjectRootElement projectImportElement, string tempPath, string testPropsFile)
        {
            try
            {
                Directory.CreateDirectory(tempPath);
                projectImportElement.Save(testPropsFile);

                return new Project(projectRootElement);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }

                if (File.Exists(testPropsFile))
                {
                    File.Delete(testPropsFile);
                }
            }
        }

        [Fact]
        public void MoveUpFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveUp(project, tree.Children[1]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test2.fs" />
                    <Compile Include="test1.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveUpFile_IsUnsuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.False(OrderingHelper.TryMoveUp(project, tree.Children[0]));
            Assert.False(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveUpFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    Folder (flags: {Folder}), FilePath: "C:\Foo\test", DisplayOrder: 3
                        File (flags: {}), FilePath: "C:\Foo\test\test3.fs", DisplayOrder: 4, ItemName: "test/test3.fs"
                        File (flags: {}), FilePath: "C:\Foo\test\test4.fs", DisplayOrder: 5, ItemName: "test/test4.fs"
                """);
            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test/test3.fs" />
                    <Compile Include="test/test4.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveUp(project, tree.Children[2]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test/test3.fs" />
                    <Compile Include="test/test4.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveDown(project, tree.Children[0]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test2.fs" />
                    <Compile Include="test1.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFile_IsUnsuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.False(OrderingHelper.TryMoveDown(project, tree.Children[1]));
            Assert.False(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    Folder (flags: {Folder}), FilePath: "C:\Foo\test", DisplayOrder: 1
                        File (flags: {}), FilePath: "C:\Foo\test\test3.fs", DisplayOrder: 2, ItemName: "test/test3.fs"
                        File (flags: {}), FilePath: "C:\Foo\test\test4.fs", DisplayOrder: 3, ItemName: "test/test4.fs"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 4, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 5, ItemName: "test2.fs"
                """);
            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test/test3.fs" />
                    <Compile Include="test/test4.fs" />
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveDown(project, tree.Children[0]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test/test3.fs" />
                    <Compile Include="test/test4.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFolder_ContainsNestedFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    Folder (flags: {Folder}), FilePath: "C:\Foo\test", DisplayOrder: 1
                        File (flags: {}), FilePath: "C:\Foo\test\test3.fs", DisplayOrder: 2, ItemName: "test/test3.fs"
                        Folder (flags: {Folder}), FilePath: "C:\Foo\test\nested", DisplayOrder: 3
                            File (flags: {}), FilePath: "C:\Foo\test\nested\nested.fs", DisplayOrder: 4, ItemName: "test/nested/nested.fs"
                        File (flags: {}), FilePath: "C:\Foo\test\test4.fs", DisplayOrder: 5, ItemName: "test/test4.fs"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 6, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 7, ItemName: "test2.fs"
                """);
            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test/test3.fs" />
                    <Compile Include="test/nested/nested.fs" />
                    <Compile Include="test/test4.fs" />
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveDown(project, tree.Children[0]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test/test3.fs" />
                    <Compile Include="test/nested/nested.fs" />
                    <Compile Include="test/test4.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveAboveFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 3, ItemName: "test3.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            var elements = OrderingHelper.GetItemElements(project, tree.Children[0], ImmutableArray<string>.Empty);

            Assert.True(OrderingHelper.TryMoveElementsAbove(project, elements, tree.Children[2]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test2.fs" />
                    <Compile Include="test1.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveBelowFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 3, ItemName: "test3.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            var elements = OrderingHelper.GetItemElements(project, tree.Children[0], ImmutableArray<string>.Empty);

            Assert.True(OrderingHelper.TryMoveElementsBelow(project, elements, tree.Children[2]));
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                    <Compile Include="test1.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void AddItem_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 3, ItemName: "test3.fs"
                """);

            var updatedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test4.fs", DisplayOrder: 3, ItemName: "test4.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 4, ItemName: "test3.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test4.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            var elements = OrderingHelper.GetItemElements(project, updatedTree.Children[2], ImmutableArray<string>.Empty);

            Assert.True(OrderingHelper.TryMoveElementsToTop(project, elements, tree), "TryMoveElementsToTop returned false.");
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test4.fs" />
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void AddItems_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 3, ItemName: "test3.fs"
                """);

            var updatedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test4.fs", DisplayOrder: 3, ItemName: "test4.fs"
                    File (flags: {}), FilePath: "C:\Foo\test5.fs", DisplayOrder: 4, ItemName: "test5.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 5, ItemName: "test3.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test4.fs" />
                    <Compile Include="test5.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            var elements =
                OrderingHelper.GetItemElements(project, updatedTree.Children[2], ImmutableArray<string>.Empty)
                .AddRange(OrderingHelper.GetItemElements(project, updatedTree.Children[3], ImmutableArray<string>.Empty));

            Assert.True(OrderingHelper.TryMoveElementsToTop(project, elements, tree), "TryMoveElementsToTop returned false.");
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test4.fs" />
                    <Compile Include="test5.fs" />
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void AddItemsInNestedFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    Folder (flags: {Folder}), FilePath: "C:\Foo\test"
                        Folder (flags: {Folder}), FilePath: "C:\Foo\test\nested"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 3, ItemName: "test3.fs"
                """);

            var updatedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    Folder (flags: {Folder}), FilePath: "C:\Foo\test", DisplayOrder: 3
                        Folder (flags: {Folder}), FilePath: "C:\Foo\test\nested", DisplayOrder: 4
                            File (flags: {}), FilePath: "C:\Foo\test\nested\test4.fs", DisplayOrder: 5, ItemName: "test\nested\test4.fs"
                            File (flags: {}), FilePath: "C:\Foo\test\tested\test5.fs", DisplayOrder: 6, ItemName: "test\nested\test5.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 7, ItemName: "test3.fs"
                """);

            var projectRootElement =
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test\nested\test4.fs" />
                    <Compile Include="test\nested\test5.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """.AsProjectRootElement();

            var project = new Project(projectRootElement);

            var elements =
                OrderingHelper.GetItemElements(project, updatedTree.Children[2].Children[0].Children[0], ImmutableArray<string>.Empty)
                .AddRange(OrderingHelper.GetItemElements(project, updatedTree.Children[2].Children[0].Children[1], ImmutableArray<string>.Empty));

            Assert.True(OrderingHelper.TryMoveElementsToTop(project, elements, tree), "TryMoveElementsToTop returned false.");
            Assert.True(project.IsDirty);

            var expected =
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test\nested\test4.fs" />
                    <Compile Include="test\nested\test5.fs" />
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>
                </Project>
                """;

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void AddFile_WithImportedFileAtTop()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                """);

            var updatedTree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 2, ItemName: "test3.fs"
                """);

            var (tempPath, testPropsFile) = CreateTempPropsFilePath();

            var projectRootElement = string.Format(
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <Import Project="{0}" />

                  <ItemGroup>
                    <Compile Include="test2.fs" />
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """, testPropsFile).AsProjectRootElement();

            var projectImportElement =
                """
                <Project>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                  </ItemGroup>
                </Project>
                """.AsProjectRootElement();

            var project = CreateProjectWithImport(projectRootElement, projectImportElement, tempPath, testPropsFile);

            var elements =
                OrderingHelper.GetItemElements(project, updatedTree.Children[0], ImmutableArray<string>.Empty)
                .AddRange(OrderingHelper.GetItemElements(project, updatedTree.Children[2], ImmutableArray<string>.Empty));

            Assert.True(OrderingHelper.TryMoveElementsToTop(project, elements, tree), "TryMoveElementsToTop returned false.");
            Assert.True(project.IsDirty);

            var expected = string.Format(
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <Import Project="{0}" />
                  <ItemGroup>
                    <Compile Include="test3.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """, testPropsFile);

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveFileUp_WithImportedFileInterspersed()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 2, ItemName: "test3.fs"
                """);

            var (tempPath, testPropsFile) = CreateTempPropsFilePath();

            var projectRootElement = string.Format(
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                  </ItemGroup>

                  <Import Project="{0}" />

                  <ItemGroup>
                    <Compile Include="test3.fs" />
                  </ItemGroup>

                </Project>
                """, testPropsFile).AsProjectRootElement();

            var projectImportElement =
                """
                <Project>
                  <ItemGroup>
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                </Project>
                """.AsProjectRootElement();

            var project = CreateProjectWithImport(projectRootElement, projectImportElement, tempPath, testPropsFile);

            Assert.True(OrderingHelper.TryMoveUp(project, tree.Children[2]));
            Assert.True(project.IsDirty);

            // The expected result here may not be the desired behavior, but it is the current behavior that we need to test for.
            // Moving test3.fs up, skips the import, but also moves above test1.fs, that is due to skipping imports during manipulation.
            var expected = string.Format(
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test3.fs" />
                    <Compile Include="test1.fs" />
                  </ItemGroup>
                  <Import Project="{0}" />
                  <ItemGroup />
                </Project>
                """, testPropsFile);

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveFileDown_WithImportedFileAtBottom()
        {
            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot}), FilePath: "C:\Foo\testing.fsproj"
                    File (flags: {}), FilePath: "C:\Foo\test1.fs", DisplayOrder: 1, ItemName: "test1.fs"
                    File (flags: {}), FilePath: "C:\Foo\test2.fs", DisplayOrder: 2, ItemName: "test2.fs"
                    File (flags: {}), FilePath: "C:\Foo\test3.fs", DisplayOrder: 2, ItemName: "test3.fs"
                """);

            var (tempPath, testPropsFile) = CreateTempPropsFilePath();

            var projectRootElement = string.Format(
                """
                <Project>

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>

                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>

                  <Import Project="{0}" />

                </Project>
                """, testPropsFile).AsProjectRootElement();

            var projectImportElement =
                """
                <Project>
                  <ItemGroup>
                    <Compile Include="test3.fs" />
                  </ItemGroup>
                </Project>
                """.AsProjectRootElement();

            var project = CreateProjectWithImport(projectRootElement, projectImportElement, tempPath, testPropsFile);

            // Assert false as nothing should change because we can't move over an import file that is at the very bottom.
            Assert.False(OrderingHelper.TryMoveDown(project, tree.Children[1]));
            Assert.False(project.IsDirty);

            var expected = string.Format(
                """
                <?xml version="1.0" encoding="utf-16"?>
                <Project>
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include="test1.fs" />
                    <Compile Include="test2.fs" />
                  </ItemGroup>
                  <Import Project="{0}" />
                </Project>
                """, testPropsFile);

            AssertEqualProject(expected, project);
        }
    }
}
