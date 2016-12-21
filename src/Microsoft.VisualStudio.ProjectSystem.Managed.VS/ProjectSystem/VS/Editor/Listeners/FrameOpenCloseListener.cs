// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(IFrameOpenCloseListener))]
    internal class FrameOpenCloseListener : OnceInitializedOnceDisposedAsync, IFrameOpenCloseListener, IVsWindowFrameEvents, IVsWindowFrameNotify2
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EditorStateModel _editorModel;
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private IVsWindowFrame _frame;
        private uint _eventCookie;

        [ImportingConstructor]
        public FrameOpenCloseListener(
            [Import(typeof(SVsServiceProvider))]IServiceProvider helper,
            EditorStateModel editorModel,
            IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject) :
            base(threadingService.JoinableTaskContext)
        {
            _serviceProvider = helper;
            _editorModel = editorModel;
            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
        }

        public Task InitializeEventsAsync() => InitializeAsync();

        public void OnFrameDestroyed(IVsWindowFrame frame)
        {
            if (_frame.Equals(frame))
            {
                _threadingService.ExecuteSynchronously(_editorModel.CloseWindowAsync);
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();
            var uiShellService = _serviceProvider.GetService<IVsUIShell7, SVsUIShell>();
            _eventCookie = uiShellService.AdviseWindowFrameEvents(this);
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                await _threadingService.SwitchToUIThread();
                var uiShellService = _serviceProvider.GetService<IVsUIShell7, SVsUIShell>();
                uiShellService.UnadviseWindowFrameEvents(_eventCookie);
            }
        }

        public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible)
        {
            // If we've already found the correct frame, return quick.
            if (_frame != null)
            {
                return;
            }

            UIThreadHelper.VerifyOnUIThread();

            // Get the path of the frame's document.
            object docPathObject;
            Verify.HResult(frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out docPathObject));
            var docPath = (string)docPathObject;

            // If the path is the path to the project file, then we've found the correct frame.
            if (StringComparers.Paths.Equals(_unconfiguredProject.FullPath, docPath))
            {
                _frame = frame;
                _frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, this);
                _editorModel.InitializeWindowFrame(_frame);
            }
        }

        public int OnClose(ref uint saveOptions)
        {
            var shouldContinue = _threadingService.ExecuteSynchronously(_editorModel.NotifyWindowMaybeClosingAsync);
            return shouldContinue ? VSConstants.S_OK : VSConstants.OLE_E_PROMPTSAVECANCELLED;
        }

        #region Unused Events

        public void OnFrameCreated(IVsWindowFrame frame)
        {
        }

        public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame)
        {
        }

        public void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen)
        {
        }

        #endregion
    }
}
