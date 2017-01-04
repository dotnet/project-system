// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class EditProjectFileCommandTests
    {
        private const long CommandId = VisualStudioStandard2kCommandId.EditProjectFile;
        private const string Extension = "proj";
        private static readonly Guid XmlEditorFactoryGuid = new Guid("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        [Fact]
        public void EditProjectFileCommand_NullProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new EditProjectFileCommand(null, IProjectFileEditorPresenterFactory.CreateLazy()));
        }

        [Fact]
        public void EditProjectFileCommand_NullModel_Throws()
        {
            Assert.Throws<ArgumentNullException>("editorState", () => new EditProjectFileCommand(UnconfiguredProjectFactory.Create(), null));
        }

        [Fact]
        public async Task EditProjectFileCommand_ValidNode_ShouldHandle()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Temp\Root\Root.proj");

            var command = new EditProjectFileCommand(unconfiguredProject, IProjectFileEditorPresenterFactory.CreateLazy());

            var result = await command.GetCommandStatusAsync(nodes, CommandId, true, "", 0);
            Assert.True(result.Handled);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
            Assert.Equal($"Edit Root.{Extension}", result.CommandText);
        }

        [Fact]
        public async Task EditProjectFileCommand_NonRootNode_ShouldntHandle()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties ()
");

            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Temp\Root\Root.proj");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var command = new EditProjectFileCommand(unconfiguredProject, IProjectFileEditorPresenterFactory.CreateLazy());

            var result = await command.GetCommandStatusAsync(nodes, CommandId, true, "", 0);
            Assert.False(result.Handled);
            Assert.Equal(CommandStatus.NotSupported, result.Status);
        }

        [Fact]
        public async Task EditProjectFileCommand_CorrectNode_CallsOpenAsync()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Temp\Root\Root.proj");
            var editorStateModel = IProjectFileEditorPresenterFactory.Create();

            var command = new EditProjectFileCommand(unconfiguredProject, new Lazy<IProjectFileEditorPresenter>(() => editorStateModel));

            var result = await command.TryHandleCommandAsync(nodes, CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);
            Assert.True(result);
            Mock.Get(editorStateModel).Verify(e => e.OpenEditorAsync(), Times.Once);
        }

        [Fact]
        public async Task EditProjectFileCommand_NotRootNode_DoesNotCallOpen()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties ()
");

            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Temp\Root\Root.proj");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var editorStateModel = IProjectFileEditorPresenterFactory.Create();

            var command = new EditProjectFileCommand(unconfiguredProject, new Lazy<IProjectFileEditorPresenter>(() => editorStateModel));

            var result = await command.TryHandleCommandAsync(nodes, CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);
            Assert.False(result);
            Mock.Get(editorStateModel).Verify(e => e.OpenEditorAsync(), Times.Never);
        }
    }
}
