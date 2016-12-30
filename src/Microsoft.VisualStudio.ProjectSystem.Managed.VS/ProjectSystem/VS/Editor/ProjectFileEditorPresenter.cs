// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// This class maintains the state machine that controls the in-memory project file editor. It manages
    /// the subscriptions to project events and the current state of the buffer.
    /// </summary>
    [Export(typeof(IProjectFileEditorPresenter))]
    internal class ProjectFileEditorPresenter : IProjectFileEditorPresenter
    {
        /// <summary>
        /// The different states that the editor can be in.
        /// </summary>
        internal enum EditorState
        {
            /// <summary>
            /// There is no open editor. No listeners are subscribed to project events.
            /// Allowed Transitions: <see cref="Initializing"/> when the user opens the editor.
            /// </summary>
            NoEditor,
            /// <summary>
            /// The editor is being initialized.
            /// Allowed Transitions: <see cref="EditorOpen"/> once initialization is completed.
            /// </summary>
            Initializing,
            /// <summary>
            /// There is an open editor, and no updates to the project file are currently scheduled.
            /// Allowed Transitions:
            /// * <see cref="EditorOpen"/> if the user makes an edit.
            /// * <see cref="BufferUpdateScheduled"/> if the user makes a change from the UI, such as adding a NuGet package.
            /// * <see cref="EditorClosing"/> if the user closes the buffer.
            /// </summary>
            EditorOpen,
            /// <summary>
            /// The changes from the open editor are being written to disk.
            /// Allowed Transitions: <see cref="EditorOpen"/> once the updated file is written out.
            /// </summary>
            WritingProjectFile,
            /// <summary>
            /// A buffer update has been scheduled.
            /// Allowed Transitions: <see cref="EditorOpen"/> when the editor has been updated.
            /// </summary>
            BufferUpdateScheduled,
            /// <summary>
            /// The editor is being closed and states being cleaned up. It should not be possible to reach this from dirty states, as
            /// the editor should be saved before then.
            /// Allowed Transitions: <see cref="NoEditor"/> when editor shutdown is complete.
            /// </summary>
            EditorClosing
        }

        private static readonly Guid XmlFactoryGuid = Guid.Parse("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        private readonly object _lock = new object();
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsShellUtilitiesHelper _shellHelper;
        private readonly ExportFactory<IProjectFileModelWatcher> _projectFileWatcherFactory;
        private readonly ExportFactory<ITextBufferStateListener> _textBufferListenerFactory;
        private readonly ExportFactory<IFrameOpenCloseListener> _frameEventsListenerFactory;
        private readonly ExportFactory<ITextBufferManager> _textBufferManagerFactory;

        // Protected so tests can access
        protected EditorState _currentState = EditorState.NoEditor;
        private IVsWindowFrame _windowFrame;
        private ITextBufferManager _textBufferManager;
        private IProjectFileModelWatcher _projectFileModelWatcher;
        private IFrameOpenCloseListener _frameEventsListener;
        private ITextBufferStateListener _textBufferStateListener;

        [ImportingConstructor]
        public ProjectFileEditorPresenter(IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IVsShellUtilitiesHelper shellHelper,
            ExportFactory<IProjectFileModelWatcher> projectFileModelWatcherFactory,
            ExportFactory<ITextBufferStateListener> textBufferListenerFactory,
            ExportFactory<IFrameOpenCloseListener> frameEventsListenerFactory,
            ExportFactory<ITextBufferManager> textBufferManagerFactory)
        {
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(shellHelper, nameof(shellHelper));
            Requires.NotNull(projectFileModelWatcherFactory, nameof(projectFileModelWatcherFactory));
            Requires.NotNull(textBufferListenerFactory, nameof(textBufferListenerFactory));
            Requires.NotNull(frameEventsListenerFactory, nameof(frameEventsListenerFactory));
            Requires.NotNull(textBufferManagerFactory, nameof(textBufferManagerFactory));

            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
            _serviceProvider = serviceProvider;
            _shellHelper = shellHelper;
            _projectFileWatcherFactory = projectFileModelWatcherFactory;
            _textBufferListenerFactory = textBufferListenerFactory;
            _frameEventsListenerFactory = frameEventsListenerFactory;
            _textBufferManagerFactory = textBufferManagerFactory;
        }

        // Initialization Logic

        /// <summary>
        /// Called by anything attempting to open a project file editor window, usually by a command. This will show the window frame, creating it if
        /// has not already been created.
        /// </summary>
        public async Task OpenEditorAsync()
        {
            // We access _windowFrame inside the lock, so we must be on the UI thread in that case
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

            // Set up the buffer manager, which will create the temp file. Nothing else in OpenEditor requires the UI thread (tasks aquire it as needed)
            // so we don't need to resume on the same thread.
            _textBufferManager = _textBufferManagerFactory.CreateExport().Value;
            await _textBufferManager.InitializeBufferAsync().ConfigureAwait(false);

            // Open and show the editor frame.
            _windowFrame = await _shellHelper.OpenDocumentWithSpecificEditorAsync(_serviceProvider, _textBufferManager.FilePath, XmlFactoryGuid, Guid.Empty).ConfigureAwait(false);

            // Set up the save listener
            _textBufferStateListener = _textBufferListenerFactory.CreateExport().Value;
            await _textBufferStateListener.InitializeListenerAsync(_textBufferManager.FilePath).ConfigureAwait(false);

            // Set up the listener that will check for when the frame is closed.
            _frameEventsListener = _frameEventsListenerFactory.CreateExport().Value;
            await _frameEventsListener.InitializeEventsAsync(_windowFrame).ConfigureAwait(false);

            // Set up the project file watcher, so changes to the project file are detected and the buffer is updated.
            _projectFileModelWatcher = _projectFileWatcherFactory.CreateExport().Value;
            _projectFileModelWatcher.InitializeModelWatcher();

            // Finally, move to the editor open state
            lock (_lock)
            {
                _currentState = EditorState.EditorOpen;
            }
        }

        // Teardown Logic

        /// <summary>
        /// Tells the editor to call close on the existing window frame.
        /// </summary>
        /// <returns>True if the close was successful, false if the window is still open</returns>
        public async Task<bool> CloseWindowAsync()
        {
            // We can't switch to editor closing here because there's still the chance that some other action can cancel
            // the editor closing, without notifying us. A later callback will get hit once the close is guaranteed to happen.
            // We do, however, assert that we're not in NoEditor, Initializing, or EditorClosing; being in any of those states
            // here would indicate a bug.
            lock (_lock)
            {
                Assumes.False(_currentState == EditorState.NoEditor ||
                    _currentState == EditorState.Initializing ||
                    _currentState == EditorState.EditorClosing);
            }
            await _threadingService.SwitchToUIThread();
            return _windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_PromptSave) == VSConstants.S_OK;
        }

        /// <summary>
        /// Starts the process of closing the project file editor, if one is open currently.
        /// </summary>
        public async Task DisposeEditorAsync()
        {
            lock (_lock)
            {
                if (_currentState == EditorState.NoEditor)
                {
                    return;
                }

                // Checking for potential dirty state and asking if the user wants to save their changes will have already occurred at this point.
                // Just go to EditorClosing.
                _currentState = EditorState.EditorClosing;
            }

            _projectFileModelWatcher?.Dispose();
            _textBufferStateListener?.Dispose();
            if (_frameEventsListener != null)
            {
                await _frameEventsListener.DisposeAsync().ConfigureAwait(false);
            }
            _textBufferManager?.Dispose();

            _projectFileModelWatcher = null;
            _frameEventsListener = null;
            _textBufferStateListener = null;
            _textBufferManager = null;

            lock (_lock)
            {
                _currentState = EditorState.NoEditor;
            }
            return;
        }

        // Update Project File Logic

        /// <summary>
        /// Schedules the project file to be updated. This is called by the ProjectFileModelWatcher, which is within a Project write lock.
        /// Because of the time-sensitive nature of that lock, instead of updating the file immediately we schedule an update as soon
        /// as the JTF can run our code.
        /// </summary>
        public JoinableTask ScheduleProjectFileUpdate()
        {
            lock (_lock)
            {
                // If the current state is writing project file, we don't want to update now, as the project will not have been fully
                // reloaded yet. We'll get called back again after the ProjectReloadManager is finished reloading the project.
                // Additionally, if there is no editor open, or if the editor is closing, don't schedule an update
                if (_currentState == EditorState.WritingProjectFile ||
                    _currentState == EditorState.NoEditor ||
                    _currentState == EditorState.EditorClosing)
                {
                    return null;
                }
                _currentState = EditorState.BufferUpdateScheduled;
                return _threadingService.JoinableTaskFactory.RunAsync(UpdateProjectFileAsync);
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
                if (_currentState != EditorState.BufferUpdateScheduled)
                {
                    return;
                }
            }

            // Set set the buffer to be unmodifiable to prevent any changes while we update the project
            await _textBufferManager.SetReadOnlyAsync(true).ConfigureAwait(true);

            // Set the buffer to default state and set the current state to editor clean
            await _textBufferManager.ResetBufferAsync().ConfigureAwait(true);

            lock (_lock)
            {
                _currentState = EditorState.EditorOpen;
            }
            await _textBufferManager.SetReadOnlyAsync(false).ConfigureAwait(true);
        }

        // Save Project File Logic

        /// <summary>
        /// Causes the dirty changes to the editor to be saved to the disk. This is called by the <seealso cref="ITextBufferStateListener"/>
        /// when it detects the user has saved the temp buffer.
        /// </summary>
        public async Task SaveProjectFileAsync()
        {
            lock (_lock)
            {
                // In order to save, the editor must not be in the process of being updated.
                Assumes.True(_currentState != EditorState.NoEditor);
                if (_currentState != EditorState.EditorOpen)
                {
                    return;
                }
                _currentState = EditorState.WritingProjectFile;
            }

            // While saving the file, we disallow edits to the project file for sync purposes. We also make sure
            // that the buffer cannot get stuck in a read-only state if SaveAsync fails for some reason.
            try
            {
                await _textBufferManager.SetReadOnlyAsync(true).ConfigureAwait(false);
                await _textBufferManager.SaveAsync().ConfigureAwait(false);
            }
            finally
            {

                await _textBufferManager.SetReadOnlyAsync(false).ConfigureAwait(false);
            }

            lock (_lock)
            {
                _currentState = EditorState.EditorOpen;
            }
        }
    }
}
