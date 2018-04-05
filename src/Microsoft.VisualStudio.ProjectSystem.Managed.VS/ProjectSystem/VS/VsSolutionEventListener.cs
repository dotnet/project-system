// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Responsible for listening to project loaded events and firing <see cref="IUnconfiguredProjectTasksService.PrioritizedProjectLoadedInHost"/> and 
    ///     <see cref="IUnconfiguredProjectTasksService.ProjectLoadedInHost"/>.
    /// </summary>
    [Export(typeof(ILoadedInHostListener))]
    internal class VsSolutionEventListener : OnceInitializedOnceDisposedAsync, IVsSolutionEvents, IVsPrioritizedSolutionEvents, ILoadedInHostListener
    {
        private readonly IVsService<IVsSolution> _solution;
        private readonly IProjectThreadingService _threadingService;
        private uint _cookie = VSConstants.VSCOOKIE_NIL;

        [ImportingConstructor]
        public VsSolutionEventListener(IVsService<SVsSolution, IVsSolution> solution, IProjectThreadingService threadingService)
            : base(threadingService.JoinableTaskContext)
        {
            _solution = solution;
            _threadingService = threadingService;
        }

        public Task StartListeningAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();

            HResult result = _solution.Value.AdviseSolutionEvents(this, out _cookie);
            if (result.Failed)
                throw result.Exception;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            await _threadingService.SwitchToUIThread();

            if (_cookie != VSConstants.VSCOOKIE_NIL)
            {
                // Due to synchronous disposable, we should be on UI thread,
                // accessing IVsService<T>.Value will enforce that.
                HResult result = _solution.Value.UnadviseSolutionEvents(_cookie);
                if (result.Failed)
                    throw result.Exception;

                _cookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            UnconfiguredProjectTasksService tasksService = GetUnconfiguredProjectTasksServiceIfApplicable(pHierarchy);
            tasksService?.OnProjectLoadedInHost();

            return HResult.OK;
        }

        public int PrioritizedOnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            UnconfiguredProjectTasksService tasksService = GetUnconfiguredProjectTasksServiceIfApplicable(pHierarchy);
            tasksService?.OnPrioritizedProjectLoadedInHost();

            return HResult.OK;
        }

        private static UnconfiguredProjectTasksService GetUnconfiguredProjectTasksServiceIfApplicable(IVsHierarchy hierarchy)
        {
            if (hierarchy is IVsBrowseObjectContext context)
            {
                // Only want to run in projects where this is applicable
                Lazy<UnconfiguredProjectTasksService, IAppliesToMetadataView> export = context.UnconfiguredProject.Services.ExportProvider.GetExport<UnconfiguredProjectTasksService, IAppliesToMetadataView>();
                if (export.AppliesTo(context.UnconfiguredProject.Capabilities))
                {
                    return export.Value;
                }
            }

            return null;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return HResult.NotImplemented;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return HResult.NotImplemented;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return HResult.NotImplemented;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return HResult.NotImplemented;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return HResult.NotImplemented;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return HResult.NotImplemented;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnBeforeCloseSolution(object pUnkReserved)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterCloseSolution(object pUnkReserved)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterMergeSolution(object pUnkReserved)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterOpeningChildren(IVsHierarchy pHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnBeforeClosingChildren(IVsHierarchy pHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterClosingChildren(IVsHierarchy pHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterRenameProject(IVsHierarchy pHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterChangeProjectParent(IVsHierarchy pHierarchy)
        {
            return HResult.NotImplemented;
        }

        public int PrioritizedOnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return HResult.NotImplemented;
        }
    }
}
