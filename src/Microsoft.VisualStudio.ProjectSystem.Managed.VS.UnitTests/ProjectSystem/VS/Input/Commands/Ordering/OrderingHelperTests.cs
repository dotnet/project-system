// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [Trait("UnitTest", "ProjectSystem")]
    public class OrderingHelperTests
    {
        private static void AssertEqualProject(string expected, Project project)
        {
            var actual = string.Empty;
            using (var writer = new System.IO.StringWriter())
            {
                project.Save(writer);
                actual = writer.ToString();
            }

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MoveUpFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
");

            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveUp(project, tree.Children[1]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test2.fs"" />
    <Compile Include=""test1.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveUpFile_IsUnsuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
");

            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.False(OrderingHelper.TryMoveUp(project, tree.Children[0]));
            Assert.False(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveUpFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
    Folder (flags: {Folder}), FilePath: ""C:\Foo\test"", DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 4, ItemName: ""test/test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 5, ItemName: ""test/test4.fs""
");
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test/test3.fs"" />
    <Compile Include=""test/test4.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveUp(project, tree.Children[2]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test/test3.fs"" />
    <Compile Include=""test/test4.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
");

            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveDown(project, tree.Children[0]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test2.fs"" />
    <Compile Include=""test1.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFile_IsUnsuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
");

            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.False(OrderingHelper.TryMoveDown(project, tree.Children[1]));
            Assert.False(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    Folder (flags: {Folder}), FilePath: ""C:\Foo\test"", DisplayOrder: 1
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 2, ItemName: ""test/test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 3, ItemName: ""test/test4.fs""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 4, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 5, ItemName: ""test2.fs""
");
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test/test3.fs"" />
    <Compile Include=""test/test4.fs"" />
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveDown(project, tree.Children[0]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test/test3.fs"" />
    <Compile Include=""test/test4.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveDownFolder_ContainsNestedFolder_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    Folder (flags: {Folder}), FilePath: ""C:\Foo\test"", DisplayOrder: 1
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 2, ItemName: ""test/test3.fs""
        Folder (flags: {Folder}), FilePath: ""C:\Foo\test\nested"", DisplayOrder: 3
            File (flags: {}), FilePath: ""C:\Foo\test\nested\nested.fs"", DisplayOrder: 4, ItemName: ""test/nested/nested.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 5, ItemName: ""test/test4.fs""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 6, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 7, ItemName: ""test2.fs""
");
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test/test3.fs"" />
    <Compile Include=""test/nested/nested.fs"" />
    <Compile Include=""test/test4.fs"" />
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveDown(project, tree.Children[0]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test/test3.fs"" />
    <Compile Include=""test/nested/nested.fs"" />
    <Compile Include=""test/test4.fs"" />
    <Compile Include=""test2.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveAboveFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
    File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 2, ItemName: ""test3.fs""
");

            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test3.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveAbove(project, tree.Children[0], tree.Children[2]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test2.fs"" />
    <Compile Include=""test1.fs"" />
    <Compile Include=""test3.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }

        [Fact]
        public void MoveBelowFile_IsSuccessful()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
    File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 2, ItemName: ""test3.fs""
");

            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test3.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var project = new Project(projectRootElement);

            Assert.True(OrderingHelper.TryMoveBelow(project, tree.Children[0], tree.Children[2]));
            Assert.True(project.IsDirty);

            var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""test2.fs"" />
    <Compile Include=""test3.fs"" />
    <Compile Include=""test1.fs"" />
  </ItemGroup>
</Project>";

            AssertEqualProject(expected, project);
        }
    }
}
