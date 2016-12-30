// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(IFrameOpenCloseListener))]
    internal class FrameOpenCloseListener : OnceInitializedOnceDisposedAsync, IFrameOpenCloseListener, IVsWindowFrameEvents, IVsSolutionEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectFileEditorPresenter _editorModel;
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private IVsWindowFrame _frame;
        private uint _eventCookie = VSConstants.VSCOOKIE_NIL;
        private uint _solutionEventCookie = VSConstants.VSCOOKIE_NIL;

        [ImportingConstructor]
        public FrameOpenCloseListener(
            [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider,
            IProjectFileEditorPresenter editorModel,
            IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject) :
            base(threadingService != null ? threadingService.JoinableTaskContext : throw new ArgumentNullException(nameof(threadingService)))
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(editorModel, nameof(editorModel));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _serviceProvider = serviceProvider;
            _editorModel = editorModel;
            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
        }

        public Task InitializeEventsAsync(IVsWindowFrame frame)
        {
            Requires.NotNull(frame, nameof(frame));
            _frame = frame;
            return InitializeAsync();
        }

        public void OnFrameDestroyed(IVsWindowFrame frame)
        {
            if (_frame.Equals(frame))
            {
                _threadingService.ExecuteSynchronously(_editorModel.DisposeEditorAsync);
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();
            var uiShellService = _serviceProvider.GetService<IVsUIShell7, SVsUIShell>();
            _eventCookie = uiShellService.AdviseWindowFrameEvents(this);

            var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            Verify.HResult(solution.AdviseSolutionEvents(this, out _solutionEventCookie));
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                await _threadingService.SwitchToUIThread();

                if (_eventCookie != VSConstants.VSCOOKIE_NIL)
                {
                    var uiShellService = _serviceProvider.GetService<IVsUIShell7, SVsUIShell>();
                    uiShellService.UnadviseWindowFrameEvents(_eventCookie);
                }

                if (_solutionEventCookie != VSConstants.VSCOOKIE_NIL)
                {
                    var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                    solution.UnadviseSolutionEvents(_solutionEventCookie);
                }
            }
        }

        // We use queryunload instead of onbeforeunload because this gives us the chance to cancel unload if the user presses cancel
        // when closing the temporary project file buffer. Otherwise, they could press cancel and the project would close with the editor
        // still open, leaving things in a very broken state.
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int shouldCancel)
        {
            var realUnconfiguredProject = pRealHierarchy.AsUnconfiguredProject();

            // Ensure that this project is actually the project we're interested in.
            if (realUnconfiguredProject == null ||
                !StringComparers.Paths.Equals(realUnconfiguredProject.FullPath, _unconfiguredProject.FullPath))
            {
                // Do not block unload of the project
                shouldCancel = 0;
                return VSConstants.S_OK;
            }

            // CloseWindowAsync returns true if closing the window succeeded. If it succeeded, we return false
            // to continue unloading the project. If the user cancelled close, we return true to cancel unload
            shouldCancel = _threadingService.ExecuteSynchronously(_editorModel.CloseWindowAsync) ? 0 : 1;
            return VSConstants.S_OK;
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

        public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible)
        {
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
