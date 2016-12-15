// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal class EditProjectFileVsFrameEvents : OnceInitializedOnceDisposedAsync, IVsWindowFrameEvents, IVsWindowFrameNotify2
    {
        private readonly IVsWindowFrame _frame;
        private readonly IServiceProvider _serviceProvider;
        private readonly EditorStateModel _editorModel;
        private readonly IProjectThreadingService _threadingService;
        private uint _eventCookie;

        public EditProjectFileVsFrameEvents(IVsWindowFrame frame,
            IServiceProvider helper,
            EditorStateModel editorModel,
            IProjectThreadingService threadingService) :
            base(threadingService.JoinableTaskContext)
        {
            _frame = frame;
            _serviceProvider = helper;
            _editorModel = editorModel;
            _threadingService = threadingService;
        }

        public Task InitializeEvents() => InitializeAsync();

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
            _frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, this);
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
        public int OnClose(ref uint pgrfSaveOptions)
        {
            return VSConstants.S_OK;
        }

        #region Unused Events

        public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame)
        {
        }

        public void OnFrameCreated(IVsWindowFrame frame)
        {
        }

        public void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen)
        {
        }

        public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible)
        {
        }

        #endregion
    }
}
