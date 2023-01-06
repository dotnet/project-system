// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Responsible for listening to project and solution loaded events and firing
    ///     <see cref="IUnconfiguredProjectTasksService.PrioritizedProjectLoadedInHost"/>,
    ///     <see cref="IUnconfiguredProjectTasksService.ProjectLoadedInHost"/> and
    ///     <see cref="LoadedInHost"/>.
    /// </summary>
    [Export(typeof(ILoadedInHostListener))]
    [Export(typeof(ISolutionService))]
    internal class VsSolutionEventListener : OnceInitializedOnceDisposedAsync, IVsSolutionEvents, IVsSolutionLoadEvents, IVsPrioritizedSolutionEvents, ILoadedInHostListener, ISolutionService
    {
        private readonly ISolutionSource _solutionSource;

        private TaskCompletionSource _loadedInHost = new();
        private System.IAsyncDisposable? _solutionEventsSubscription;

        [ImportingConstructor]
        public VsSolutionEventListener(ISolutionSource solutionSource, JoinableTaskContext joinableTaskContext)
            : base(new JoinableTaskContextNode(joinableTaskContext))
        {
            _solutionSource = solutionSource;
        }

        public Task LoadedInHost
        {
            get { return _loadedInHost.Task; }
        }

        public Task StartListeningAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            _solutionEventsSubscription = await _solutionSource.SubscribeAsync(this, cancellationToken);

            // In the situation where the solution has already been loaded by the time we're 
            // initialized, we need to make sure we set LoadedInHost as we will have missed the 
            // event. This can occur when the first CPS project is loaded due to reload of 
            // an unloaded project or the first CPS project is loaded in the Add New/Existing Project 
            // case.
            Verify.HResult(_solutionSource.Solution.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded, out object isFullyLoaded));

            if ((bool)isFullyLoaded)
            {
                _loadedInHost.TrySetResult();
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                Assumes.NotNull(_solutionEventsSubscription);

                await JoinableFactory.SwitchToMainThreadAsync();

                await _solutionEventsSubscription.DisposeAsync();

                _loadedInHost.TrySetCanceled();
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            UnconfiguredProjectTasksService? tasksService = GetUnconfiguredProjectTasksServiceIfApplicable(pHierarchy);
            tasksService?.OnProjectLoadedInHost();

            return HResult.OK;
        }

        public int PrioritizedOnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            UnconfiguredProjectTasksService? tasksService = GetUnconfiguredProjectTasksServiceIfApplicable(pHierarchy);
            tasksService?.OnPrioritizedProjectLoadedInHost();

            return HResult.OK;
        }

        public int OnAfterBackgroundSolutionLoadComplete()
        {
            _loadedInHost.TrySetResult();
            return HResult.OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            _loadedInHost = new TaskCompletionSource();

            return HResult.OK;
        }

        private static UnconfiguredProjectTasksService? GetUnconfiguredProjectTasksServiceIfApplicable(IVsHierarchy hierarchy)
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

        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            return HResult.NotImplemented;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return HResult.NotImplemented;
        }

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return HResult.NotImplemented;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return HResult.NotImplemented;
        }

        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return HResult.NotImplemented;
        }
    }
}
