// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor.Listeners
{
    [Export(typeof(ITextBufferStateListener))]
    internal class TextBufferStateListener : OnceInitializedOnceDisposedAsync, ITextBufferStateListener
    {
        private readonly EditorStateModel _editorState;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly ITextBufferManager _textBufferManager;
        private readonly IMsBuildAccessor _msbuildAccessor;

        private WindowPane _windowPane;
        private bool _lastDirtyState;
        private bool _forceUpdateState = true;
        private int _cleanReiteratedVersionNumber;
        private int _lastReiteratedVersionNumber;

        [ImportingConstructor]
        public TextBufferStateListener(EditorStateModel editorState,
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            IMsBuildAccessor msbuildAccessor,
            ITextBufferManager textBufferManager) :
            base(threadingService.JoinableTaskContext)
        {
            _editorState = editorState;
            _unconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
            _msbuildAccessor = msbuildAccessor;
            _textBufferManager = textBufferManager;
        }

        public async Task InitializeAsync(WindowPane hostFrame)
        {
            _windowPane = hostFrame;
            await InitializeAsync().ConfigureAwait(false);
        }
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();
            _textBufferManager.TextBuffer.Changed += Buffer_Changed;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                await _threadingService.SwitchToUIThread();
                _textBufferManager.TextBuffer.Changed -= Buffer_Changed;
            }
        }

        private void Buffer_Changed(object sender, Text.TextContentChangedEventArgs e)
        {
            UIThreadHelper.VerifyOnUIThread();
            _lastReiteratedVersionNumber = e.AfterVersion.ReiteratedVersionNumber;
            _threadingService.ExecuteSynchronously(SetDirtyStateAsync);
        }

        public async Task SaveAsync()
        {
            // We use the UI thread to avoid having to lock the reiterated version numbers
            await _threadingService.SwitchToUIThread();

            // Return immediately if we haven't made any changes to the buffer
            if (_cleanReiteratedVersionNumber == _lastReiteratedVersionNumber) return;

            var projectText = await ReadProjectFileAsync().ConfigureAwait(false);
            await _msbuildAccessor.SaveProjectXmlAsync(projectText).ConfigureAwait(false);

            await ForceBufferStateCleanAsync().ConfigureAwait(false);
        }

        public async Task ForceBufferStateCleanAsync()
        {
            await _threadingService.SwitchToUIThread();
            _cleanReiteratedVersionNumber = _lastReiteratedVersionNumber;
            _lastDirtyState = false;
            _forceUpdateState = true;
            await SetDirtyStateAsync().ConfigureAwait(false);
        }

        private async Task SetDirtyStateAsync()
        {
            // The dirty state follows the following 2 checks:
            // 1. If the in-memory copy is dirty (ie, the the last clean reiterated version number does not equal
            // the current reiterated version number), then the buffer is dirty.
            // 2. If the buffer is clean but the file on disk is different from the in-memory model, then the buffer
            // is dirty. The reasoning behind this is if a user opened the file on disk and its contents differ from
            // what is displayed inside VS, the buffer should be marked as dirty.
            // 3. If neither of the above rules are satisfied, then the buffer is clean.

            // Checking whether the project is dirty is expensive to do on every change. Only check if the
            // document is not dirty itself
            await _threadingService.SwitchToUIThread();
            var isDirty = _lastReiteratedVersionNumber != _cleanReiteratedVersionNumber;
            if (!isDirty)
            {
                isDirty = await _unconfiguredProject.GetIsDirtyAsync().ConfigureAwait(true);
            }

            if (isDirty != _lastDirtyState || _forceUpdateState)
            {
                var windowFrame = _windowPane.GetService<IVsWindowFrame, SVsWindowFrame>();
                // If there's no visible area, there won't yet be a window pane, so return and set _forceUpdateState. This
                // will make sure that we have the correct state on the next user change.
                if (windowFrame == null)
                {
                    _forceUpdateState = true;
                    return;
                }

                windowFrame.SetProperty((int)__VSFPROPID2.VSFPROPID_OverrideDirtyState, isDirty);
                _editorState.SetEditorDirty(isDirty);
                _lastDirtyState = isDirty;
                _forceUpdateState = false;

                // If the buffer isn't dirty, then we force the gutter green. The gutter goes green when the FileActionOccurred event is triggered.
                // In the future, the editor will be able to expose an API for us to hook here, but until then, we manually reflect in and call
                // the event.
                if (!isDirty)
                {
                    ITextDocument doc;
                    var getResult = _textBufferManager.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out doc);
                    var eventDelegate = (MulticastDelegate)doc.GetType().GetField("FileActionOccurred", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(doc);
                    var fileAction = new TextDocumentFileActionEventArgs(_unconfiguredProject.FullPath, DateTime.Now, FileActionTypes.ContentSavedToDisk);
                    eventDelegate.DynamicInvoke(new object[] { doc, fileAction });
                }
            }
        }

        private async Task<string> ReadProjectFileAsync()
        {
            await _threadingService.SwitchToUIThread();
            return _textBufferManager.TextBuffer.CurrentSnapshot.GetText();
        }
    }
}
