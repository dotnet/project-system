// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [Trait("UnitTest", "ProjectSystem")]
    public class MoveDownCommandTests : AbstractMoveCommandTests
    {
        [Fact]
        public async Task GetCommandStatusAsync_File_ReturnsStatusEnabled()
        {
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

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]); // test1.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_File_ReturnsStatusNinched()
        {
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

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[1]); // test2.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Ninched));
            Assert.False(result.Status.HasFlag(CommandStatus.Enabled));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FileInFolder_ReturnsStatusEnabled()
        {
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test\test3.fs"" />
    <Compile Include=""test\test4.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 4, ItemName: ""test\test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 5, ItemName: ""test\test4.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[2].Children[0]); // test3.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FileInFolder_ReturnsStatusNinched()
        {
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test\test3.fs"" />
    <Compile Include=""test\test4.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 4, ItemName: ""test\test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 5, ItemName: ""test\test4.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[2].Children[1]); // test4.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Ninched));
            Assert.False(result.Status.HasFlag(CommandStatus.Enabled));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderOverFolder_ReturnsStatusEnabled()
        {
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test\test3.fs"" />
    <Compile Include=""test\test4.fs"" />
    <Compile Include=""test2\test5.fs"" />
    <Compile Include=""test2\test6.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2, ItemName: ""test2.fs""
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 4, ItemName: ""test\test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 5, ItemName: ""test\test4.fs""
    Folder (flags: {Folder}), DisplayOrder: 6
        File (flags: {}), FilePath: ""C:\Foo\test2\test5.fs"", DisplayOrder: 7, ItemName: ""test2\test5.fs""
        File (flags: {}), FilePath: ""C:\Foo\test2\test6.fs"", DisplayOrder: 8, ItemName: ""test2\test6.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[2]); // first folder

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderOverFile_ReturnsStatusEnabled()
        {
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test\test3.fs"" />
    <Compile Include=""test\test4.fs"" />
    <Compile Include=""test1.fs"" />
    <Compile Include=""test2.fs"" />
    <Compile Include=""test2\test5.fs"" />
    <Compile Include=""test2\test6.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    Folder (flags: {Folder}), DisplayOrder: 1
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 2, ItemName: ""test\test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 3, ItemName: ""test\test4.fs""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 4, ItemName: ""test1.fs""
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 5, ItemName: ""test2.fs""
    Folder (flags: {Folder}), DisplayOrder: 6
        File (flags: {}), FilePath: ""C:\Foo\test2\test5.fs"", DisplayOrder: 7, ItemName: ""test2\test5.fs""
        File (flags: {}), FilePath: ""C:\Foo\test2\test6.fs"", DisplayOrder: 8, ItemName: ""test2\test6.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]); // first folder

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_Folder_ReturnsStatusNinched()
        {
            var projectRootElement = @"
<Project>

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""test\test3.fs"" />
    <Compile Include=""test\test4.fs"" />
    <Compile Include=""test2\test5.fs"" />
    <Compile Include=""test2\test6.fs"" />
  </ItemGroup>

</Project>
".AsProjectRootElement();

            var command = CreateAbstractInstance(accessor: IProjectAccessorFactory.Create(projectRootElement));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    Folder (flags: {Folder}), DisplayOrder: 1
        File (flags: {}), FilePath: ""C:\Foo\test\test3.fs"", DisplayOrder: 2, ItemName: ""test\test3.fs""
        File (flags: {}), FilePath: ""C:\Foo\test\test4.fs"", DisplayOrder: 3, ItemName: ""test\test4.fs""
    Folder (flags: {Folder}), DisplayOrder: 4
        File (flags: {}), FilePath: ""C:\Foo\test2\test5.fs"", DisplayOrder: 5, ItemName: ""test2\test5.fs""
        File (flags: {}), FilePath: ""C:\Foo\test2\test6.fs"", DisplayOrder: 6, ItemName: ""test2\test6.fs""
");

            var nodes = ImmutableHashSet.Create(tree.Children[1]); // second folder

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Ninched));
            Assert.False(result.Status.HasFlag(CommandStatus.Enabled));
        }

        override internal long GetCommandId() => ManagedProjectSystemPackage.MoveDownCmdId;

        override internal AbstractMoveCommand CreateInstance(IPhysicalProjectTree projectTree, Shell.SVsServiceProvider serviceProvider, ConfiguredProject configuredProject, IProjectAccessor accessor)
        {
            return new MoveDownCommand(projectTree, serviceProvider, configuredProject, accessor);
        }
    }
}
