// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class ProjectFileModelWatcherTests
    {
        [Fact]
        public void ProjectFileModelWatcher_NullEditorState_Throws()
        {
            Assert.Throws<ArgumentNullException>("editorState", () => new ProjectFileModelWatcher(null, UnconfiguredProjectFactory.Create()));
        }

        [Fact]
        public void ProjectFileModelWatcher_NullUnconfiguredProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new ProjectFileModelWatcher(IEditorStateModelFactory.Create(), null));
        }

        [Fact]
        public void ProjectFileModelWatcher_Initialize_SubscribesAndCausesCallback()
        {
            var editorState = IEditorStateModelFactory.Create();
            var unconfiguredProject = UnconfiguredProjectFactory.CreateWithUnconfiguredProjectAdvanced();

            var watcher = new ProjectFileModelWatcher(editorState, unconfiguredProject);

            watcher.InitializeModelWatcher();

            var unconfiguredProjectAdvanced = Mock.Get(unconfiguredProject).As<UnconfiguredProjectAdvanced>();
            unconfiguredProjectAdvanced.Raise(u => u.ChangingProjectFile += null, EventArgs.Empty);

            Mock.Get(editorState).Verify(e => e.ScheduleProjectFileUpdate(), Times.Once);
        }

        [Fact]
        public void ProjectFileModelWatcher_Dispose_Unsubscribes()
        {
            var editorState = IEditorStateModelFactory.Create();
            var unconfiguredProject = UnconfiguredProjectFactory.CreateWithUnconfiguredProjectAdvanced();

            var watcher = new ProjectFileModelWatcher(editorState, unconfiguredProject);

            watcher.InitializeModelWatcher();

            var unconfiguredProjectAdvanced = Mock.Get(unconfiguredProject).As<UnconfiguredProjectAdvanced>();

            unconfiguredProjectAdvanced.Raise(u => u.ChangingProjectFile += null, EventArgs.Empty);
            Mock.Get(editorState).Verify(e => e.ScheduleProjectFileUpdate(), Times.Once);

            watcher.Dispose();

            // Still should have only had one call to ScheduleProjectFileUpdate, as the watcher should be unsubscribed
            unconfiguredProjectAdvanced.Raise(u => u.ChangingProjectFile += null, EventArgs.Empty);
            Mock.Get(editorState).Verify(e => e.ScheduleProjectFileUpdate(), Times.Once);
        }
    }
}
