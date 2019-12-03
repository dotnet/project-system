// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <inheritdoc cref="ISolutionService"/>
    [Export(typeof(ISolutionService))]
    [Export(typeof(IPackageService))]
    internal sealed class SolutionService : ISolutionService, IVsSolutionEvents, IVsPrioritizedSolutionEvents, IPackageService
    {
        private readonly JoinableTaskContext _context;
        private IVsSolution? _solution;
        private uint _cookie = VSConstants.VSCOOKIE_NIL;

        [ImportingConstructor]
        public SolutionService(JoinableTaskContext context)
        {
            _context = context;
        }

        public bool IsSolutionClosing { get; private set; }

        public async Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            Assumes.Null(_solution);
            Assumes.True(_context.IsOnMainThread, "Must be on UI thread");

            _solution = await asyncServiceProvider.GetServiceAsync<IVsSolution, IVsSolution>();

            Verify.HResult(_solution.AdviseSolutionEvents(this, out _cookie));
        }

        // We handle both prioritized and regular before/after events to update state as early as possible,
        // and ensure we set the value regardless of whether the caller supports one or both interfaces.

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)                                 => UpdateClosing(false);
        public int OnBeforeCloseSolution(object pUnkReserved)                                                 => UpdateClosing(true);
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)                                    => HResult.OK;
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)              => HResult.OK;
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)                                => HResult.OK;
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)               => HResult.OK;
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)                        => HResult.OK;
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)            => HResult.OK;
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)                                => HResult.OK;
        public int OnAfterCloseSolution(object pUnkReserved)                                                  => HResult.OK;

        public int PrioritizedOnAfterOpenSolution(object pUnkReserved, int fNewSolution)                      => UpdateClosing(false);
        public int PrioritizedOnBeforeCloseSolution(object pUnkReserved)                                      => UpdateClosing(true);
        public int PrioritizedOnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)                         => HResult.OK;
        public int PrioritizedOnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)                     => HResult.OK;
        public int PrioritizedOnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)    => HResult.OK;
        public int PrioritizedOnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => HResult.OK;
        public int PrioritizedOnAfterCloseSolution(object pUnkReserved)                                       => HResult.OK;
        public int PrioritizedOnAfterMergeSolution(object pUnkReserved)                                       => HResult.OK;
        public int PrioritizedOnBeforeOpeningChildren(IVsHierarchy pHierarchy)                                => HResult.OK;
        public int PrioritizedOnAfterOpeningChildren(IVsHierarchy pHierarchy)                                 => HResult.OK;
        public int PrioritizedOnBeforeClosingChildren(IVsHierarchy pHierarchy)                                => HResult.OK;
        public int PrioritizedOnAfterClosingChildren(IVsHierarchy pHierarchy)                                 => HResult.OK;
        public int PrioritizedOnAfterRenameProject(IVsHierarchy pHierarchy)                                   => HResult.OK;
        public int PrioritizedOnAfterChangeProjectParent(IVsHierarchy pHierarchy)                             => HResult.OK;
        public int PrioritizedOnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)                   => HResult.OK;

        private HResult UpdateClosing(bool isClosing)
        {
            IsSolutionClosing = isClosing;
            return HResult.OK;
        }

        public void Dispose()
        {
            Assumes.True(_context.IsOnMainThread, "Must be on UI thread");

            if (_cookie != VSConstants.VSCOOKIE_NIL && _solution != null)
            {
                Verify.HResult(_solution.UnadviseSolutionEvents(_cookie));

                _cookie = VSConstants.VSCOOKIE_NIL;
            }
        }
    }
}
