// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(IProjectFileModelWatcher))]
    class ProjectFileModelWatcher : OnceInitializedOnceDisposed, IProjectFileModelWatcher
    {
        private readonly IProjectFileEditorPresenter _editorState;
        private readonly UnconfiguredProjectAdvanced _unconfiguredProject;

        [ImportingConstructor]
        public ProjectFileModelWatcher(IProjectFileEditorPresenter editorState, UnconfiguredProject unconfiguredProject)
            : base(synchronousDisposal: true)
        {
            Requires.NotNull(editorState, nameof(editorState));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _editorState = editorState;
            _unconfiguredProject = (UnconfiguredProjectAdvanced)unconfiguredProject;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unconfiguredProject.ChangingProjectFile -= ChangingProjectFile;
            }
        }

        protected override void Initialize()
        {
            _unconfiguredProject.ChangingProjectFile += ChangingProjectFile;
        }

        private void ChangingProjectFile(object sender, EventArgs e)
        {
            _editorState.ScheduleProjectFileUpdate();
        }

        public void InitializeModelWatcher()
        {
            EnsureInitialized(true);
        }
    }
}
