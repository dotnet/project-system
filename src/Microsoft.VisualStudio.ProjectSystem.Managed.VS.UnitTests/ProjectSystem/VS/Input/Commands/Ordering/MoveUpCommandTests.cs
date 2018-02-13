// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Packaging;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [Trait("UnitTest", "ProjectSystem")]
    public class MoveUpCommandTests : AbstractMoveCommandTests
    {
        [Fact]
        public async Task GetCommandStatusAsync_File_ReturnsStatusEnabled()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2
");

            var nodes = ImmutableHashSet.Create(tree.Children[1]); // test2.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_File_ReturnsStatusNinched()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]); // test1.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Ninched));
            Assert.False(result.Status.HasFlag(CommandStatus.Enabled));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FileInFolder_ReturnsStatusEnabled()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 4
        File (flags: {}), FilePath: ""C:\Foo\test4.fs"", DisplayOrder: 5
");

            var nodes = ImmutableHashSet.Create(tree.Children[2].Children[1]); // test4.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FileInFolder_ReturnsStatusNinched()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 4
        File (flags: {}), FilePath: ""C:\Foo\test4.fs"", DisplayOrder: 5
");

            var nodes = ImmutableHashSet.Create(tree.Children[2].Children[0]); // test3.fs

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Ninched));
            Assert.False(result.Status.HasFlag(CommandStatus.Enabled));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderOverFolder_ReturnsStatusEnabled()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 4
        File (flags: {}), FilePath: ""C:\Foo\test4.fs"", DisplayOrder: 5
    Folder (flags: {Folder}), DisplayOrder: 6
        File (flags: {}), FilePath: ""C:\Foo\test5.fs"", DisplayOrder: 7
        File (flags: {}), FilePath: ""C:\Foo\test6.fs"", DisplayOrder: 8
");

            var nodes = ImmutableHashSet.Create(tree.Children[3]); // second folder

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderOverFile_ReturnsStatusEnabled()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    File (flags: {}), FilePath: ""C:\Foo\test1.fs"", DisplayOrder: 1
    File (flags: {}), FilePath: ""C:\Foo\test2.fs"", DisplayOrder: 2
    Folder (flags: {Folder}), DisplayOrder: 3
        File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 4
        File (flags: {}), FilePath: ""C:\Foo\test4.fs"", DisplayOrder: 5
    Folder (flags: {Folder}), DisplayOrder: 6
        File (flags: {}), FilePath: ""C:\Foo\test5.fs"", DisplayOrder: 7
        File (flags: {}), FilePath: ""C:\Foo\test6.fs"", DisplayOrder: 8
");

            var nodes = ImmutableHashSet.Create(tree.Children[2]); // first folder

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Enabled));
            Assert.False(result.Status.HasFlag(CommandStatus.Ninched));
        }

        [Fact]
        public async Task GetCommandStatusAsync_Folder_ReturnsStatusNinched()
        {
            var command = CreateAbstractInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\testing.fsproj""
    Folder (flags: {Folder}), DisplayOrder: 1
        File (flags: {}), FilePath: ""C:\Foo\test3.fs"", DisplayOrder: 2
        File (flags: {}), FilePath: ""C:\Foo\test4.fs"", DisplayOrder: 3
    Folder (flags: {Folder}), DisplayOrder: 4
        File (flags: {}), FilePath: ""C:\Foo\test5.fs"", DisplayOrder: 5
        File (flags: {}), FilePath: ""C:\Foo\test6.fs"", DisplayOrder: 6
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]); // first folder

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Status.HasFlag(CommandStatus.Ninched));
            Assert.False(result.Status.HasFlag(CommandStatus.Enabled));
        }

        override internal long GetCommandId() => ManagedProjectSystemPackage.MoveUpCmdId;

        override internal AbstractMoveCommand CreateInstance(IPhysicalProjectTree projectTree, Shell.SVsServiceProvider serviceProvider, ConfiguredProject configuredProject)
        {
            return new MoveUpCommand(projectTree, serviceProvider, configuredProject);
        }
    }
}
