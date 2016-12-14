// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Editor.Listeners;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Composition;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// This class maintains the state machine that controls the in-memory project file editor. It manages
    /// the subscriptions to project events and the current state of the buffer.
    /// </summary>
    [Export]
    internal class EditorStateModel
    {
        /// <summary>
        /// The different states that the editor can be in.
        /// </summary>
        private enum EditorState
        {
            /// <summary>
            /// There is no open editor. No listeners are subscribed to project events.
            /// Allowed Transitions: <see cref="Initializing"/> when the user opens the editor.
            /// </summary>
            NoEditor,
            /// <summary>
            /// The editor is being initialized.
            /// Allowed Transitioins: <see cref="EditorClean"/> once initialization is completed.
            /// </summary>
            Initializing,
            /// <summary>
            /// There is an open editor, and it has no unsaved changes.
            /// Allowed Transitions:
            /// * <see cref="EditorDirty"/> if the user makes an edit.
            /// * <see cref="ProjectFileUpdateScheduledFromClean"/> if the user makes a change from the UI, ushc as adding a NuGet package.
            /// * <see cref="EditorClosing"/> if the user closes the buffer.
            /// </summary>
            EditorClean,
            /// <summary>
            /// There is an open editor, and it has one or more unsaved changes.
            /// Allowed Transitions:
            /// * <see cref="WritingProjectFile"/> if the user presses save.
            /// * <see cref="ProjectFileUpdateScheduledFromDirty"/> if the user makes a change from the UI, such as adding a NuGet package.
            /// * <see cref="EditorClean"/> if the user undoes their edits.
            /// </summary>
            EditorDirty,
            /// <summary>
            /// The changes from the open editor are being written to disk.
            /// Allowed Transitions: <see cref="EditorClean"/> once the updated file is written out.
            /// </summary>
            WritingProjectFile,
            /// <summary>
            /// A project file update has been scheduled. Before the update began, the state was <see cref="EditorClean"/>.
            /// Allowed Transitions: <see cref="ProjectFileChangingFromClean"/> when the editor update is run.
            /// </summary>
            ProjectFileUpdateScheduledFromClean,
            /// <summary>
            /// A project file update has been scheduled. Before the update began, the state was <see cref="EditorDirty"/>.
            /// Allowed Transitions: <see cref="ProjectFileChangingFromDirty"/> when the editor update is run.
            /// </summary>
            ProjectFileUpdateScheduledFromDirty,
            /// <summary>
            /// The project is being changed programmatically. The open editor was originally in a clean state.
            /// Allowed Transitions: <see cref="EditorClean"/> when the editor is updated.
            /// </summary>
            ProjectFileChangingFromClean,
            /// <summary>
            /// The project is being changed programmatically. The open editor was originally in a dirty state.
            /// Allowed Transitions:
            /// * <see cref="EditorDirty"/> if the user cancels the update.
            /// * <see cref="ProjectFileChangingFromClean"/> if the user discards changes.
            /// </summary>
            ProjectFileChangingFromDirty,
            /// <summary>
            /// The editor is being closed and states being cleaned up. It should not be possible to reach this from dirty states, as
            /// the editor should be saved before then.
            /// Allowed Transitions: <see cref="NoEditor"/> when editor shutdown is complete.
            /// </summary>
            EditorClosing
        }

        private readonly object _lock = new object();
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsShellUtilitiesHelper _shellHelper;
        private readonly IExportFactory<IProjectFileModelWatcher> _projectFileWatcherFactory;
        private readonly IExportFactory<ITextBufferStateListener> _textBufferListenerFactory;
        private readonly ITextBufferManager _textBufferManager;

        private EditorState _currentState = EditorState.NoEditor;
        private IVsWindowFrame _windowFrame;
        private IProjectFileModelWatcher _projectFileModelWatcher;
        private EditProjectFileVsFrameEvents _frameEventsListener;
        private ITextBufferStateListener _textBufferStateListener;

        [ImportingConstructor]
        public EditorStateModel(IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IVsShellUtilitiesHelper shellHelper,
            IExportFactory<IProjectFileModelWatcher> projectFileModelWatcherFactory,
            IExportFactory<ITextBufferStateListener> textBufferListenerFactory,
            ITextBufferManager textBufferManager)
        {
            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
            _serviceProvider = serviceProvider;
            _shellHelper = shellHelper;
            _projectFileWatcherFactory = projectFileModelWatcherFactory;
            _textBufferListenerFactory = textBufferListenerFactory;
            _textBufferManager = textBufferManager;
        }

        #region Initialization/Destruction

        /// <summary>
        /// Called by anything attempting to open a project file editor window, usually by a command. This will show the window frame, creating it if
        /// has not already been created.
        /// </summary>
        public async Task OpenEditorAsync()
        {
            await _threadingService.SwitchToUIThread();
            lock (_lock)
            {
                // If the editor is already open, just show it and return
                if (_currentState != EditorState.NoEditor)
                {
                    // If we're initializing, _windowFrame might be null. In that case, when the initialization code
                    // is done, it'll take care of showing the frame.
                    _windowFrame?.Show();
                    return;
                }
                _currentState = EditorState.Initializing;
            }

            // First, open the frame
            var loadedProjectEditorGuid = Guid.Parse(LoadedProjectFileEditorFactory.EditorFactoryGuid);
            _windowFrame = _shellHelper.OpenDocumentWithSpecificEditor(_serviceProvider, _unconfiguredProject.FullPath, loadedProjectEditorGuid, Guid.Empty);

            // Set up the project file watcher, so changes to the project file are detected and the buffer is updated.
            _projectFileModelWatcher = _projectFileWatcherFactory.CreateExport();
            _projectFileModelWatcher.Initialize();

            // Finally, show the frame and move to EditorClean in lockstep
            lock (_lock)
            {
                _windowFrame.Show();
                _currentState = EditorState.EditorClean;
                _projectFileModelWatcher.Initialize();
            }
        }

        /// <summary>
        /// Called from the editor wrapper. This sets up the listener that controls the dirty state and saving commands.
        /// </summary>
        public async Task InitializeTextBufferStateListenerAsync(WindowPane hostPane)
        {
            _textBufferStateListener = _textBufferListenerFactory.CreateExport();
            await _textBufferStateListener.InitializeAsync(hostPane).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the process of closing the project file editor, if one is open currently.
        /// </summary>
        public async Task CloseWindowAsync()
        {
            lock (_lock)
            {
                if (_currentState == EditorState.NoEditor) return;

                // If there was no changes, then we just close the editor
                if (_currentState == EditorState.EditorClean || _currentState == EditorState.ProjectFileChangingFromClean)
                {
                    _currentState = EditorState.EditorClosing;
                }

                Requires.Range(_currentState != EditorState.EditorDirty && _currentState != EditorState.ProjectFileChangingFromDirty, nameof(_currentState));
            }

            _projectFileModelWatcher?.Dispose();
            _textBufferStateListener?.Dispose();
            await (_frameEventsListener?.DisposeAsync() ?? Task.CompletedTask).ConfigureAwait(false);
            _projectFileModelWatcher = null;
            _frameEventsListener = null;
            _textBufferStateListener = null;

            lock (_lock)
            {
                _currentState = EditorState.NoEditor;
            }
            return;
        }

        #endregion

        #region Update Project File

        /// <summary>
        /// Schedules the project file to be updated. This is called by the ProjectFileModelWatcher, which is within a Project write lock.
        /// Because of the time-sensitive nature of that lock, instead of updating the file immediately we schedule an update as soon
        /// as the JTF can run our code.
        /// </summary>
        public void ScheduleProjectFileUpdate()
        {
            lock (_lock)
            {
                _currentState = _currentState == EditorState.EditorDirty ?
                    EditorState.ProjectFileUpdateScheduledFromDirty : EditorState.ProjectFileUpdateScheduledFromClean;
                _threadingService.JoinableTaskFactory.RunAsync(UpdateProjectFileAsync);
            }
        }

        /// <summary>
        /// Updates the content of the project file to be the latest msbuild text.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateProjectFileAsync()
        {
            // We do this on the UI thread to ensure that between setting the state to a variant of ProjectFileChanging and setting the window
            // frame to readonly, the user can't do any input (and potentially cause a conflict).
            await _threadingService.SwitchToUIThread();
            lock (_lock)
            {
                // Only update the project file if we have scheduled an update. Any other state is either already updating the buffer,
                // or in the processes of closing the buffer.
                if (_currentState != EditorState.ProjectFileUpdateScheduledFromClean
                    && _currentState != EditorState.ProjectFileUpdateScheduledFromDirty)
                {
                    return;
                }

                _currentState = _currentState == EditorState.ProjectFileUpdateScheduledFromDirty ?
                    EditorState.ProjectFileChangingFromDirty : EditorState.ProjectFileChangingFromClean;
            }

            // Set set the buffer to be unmodifiable to prevent any changes while we update the project
            await SetBufferReadonlyAsync(true).ConfigureAwait(true);

            // TODO: Handle dirty state

            // Set the buffer to default state and set the current state to editor clean
            _textBufferManager.ResetBuffer();
            await _textBufferStateListener.ForceBufferStateCleanAsync().ConfigureAwait(true);
            lock (_lock)
            {
                _currentState = EditorState.EditorClean;
            }
            await SetBufferReadonlyAsync(false).ConfigureAwait(true);
        }

        #endregion

        /// <summary>
        /// Causes the dirty changes to the editor to be saved to the disk.
        /// </summary>
        public async Task SaveProjectFileAsync()
        {
            lock (_lock)
            {
                // In order to save, the editor must be in the dirty state.
                if (_currentState != EditorState.EditorDirty) return;
                _currentState = EditorState.WritingProjectFile;
            }

            // While saving the file, we disallow edits to the project file for sync purposes.
            await SetBufferReadonlyAsync(true).ConfigureAwait(false);
            await _textBufferStateListener.SaveAsync().ConfigureAwait(false);

            lock (_lock)
            {
                _currentState = EditorState.EditorClean;
            }
            await SetBufferReadonlyAsync(false).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the current editor state to either dirty or clean.
        /// </summary>
        public void SetEditorDirty(bool isDirty)
        {
            lock (_lock)
            {
                // If the current state isn't clean or dirty, some other change is happening, and we should not touch the current state
                if (_currentState != EditorState.EditorClean && _currentState != EditorState.EditorDirty) return;

                _currentState = _currentState == EditorState.EditorClean ? EditorState.EditorDirty : EditorState.EditorClean;
            }
        }

        private async Task SetBufferReadonlyAsync(bool readOnly)
        {
            await _threadingService.SwitchToUIThread();
            _textBufferManager.SetStateFlags(readOnly ? (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY : 0);
        }
    }
}
