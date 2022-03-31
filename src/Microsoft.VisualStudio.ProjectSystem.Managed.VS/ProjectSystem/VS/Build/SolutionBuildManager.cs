// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    [Export(typeof(ISolutionBuildManager))]
    internal sealed class SolutionBuildManager : OnceInitializedOnceDisposedAsync, ISolutionBuildManager
    {
        private readonly IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> _vsSolutionBuildManagerService;

        private IVsSolutionBuildManager2? _vsSolutionBuildManager2;
        private IVsSolutionBuildManager3? _vsSolutionBuildManager3;

        [ImportingConstructor]
        public SolutionBuildManager(
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> solutionBuildManagerService,
            JoinableTaskContext joinableTaskContext)
            : base(new(joinableTaskContext))
        {
            _vsSolutionBuildManagerService = solutionBuildManagerService;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            _vsSolutionBuildManager2 = await _vsSolutionBuildManagerService.GetValueAsync(cancellationToken);
            _vsSolutionBuildManager3 = (IVsSolutionBuildManager3)_vsSolutionBuildManager2;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        public async Task<IAsyncDisposable> SubscribeSolutionEventsAsync(IVsUpdateSolutionEvents eventListener)
        {
            await InitializeAsync();

            Assumes.NotNull(_vsSolutionBuildManager2);
            Assumes.NotNull(_vsSolutionBuildManager3);

            await JoinableFactory.SwitchToMainThreadAsync();

            uint cookie3 = VSConstants.VSCOOKIE_NIL;

            HResult.Verify(
                _vsSolutionBuildManager2.AdviseUpdateSolutionEvents(eventListener, out uint cookie),
                $"Error advising solution events in {typeof(SolutionBuildManager)}.");

            if (eventListener is IVsUpdateSolutionEvents3 events3)
            {
                HResult.Verify(
                    _vsSolutionBuildManager3.AdviseUpdateSolutionEvents3(events3, out cookie3),
                    $"Error advising solution events 3 in {typeof(SolutionBuildManager)}.");
            }

            return new AsyncDisposable(async () =>
            {
                await JoinableFactory.SwitchToMainThreadAsync();

                HResult.Verify(
                    _vsSolutionBuildManager2.UnadviseUpdateSolutionEvents(cookie),
                    $"Error unadvising solution events in {typeof(SolutionBuildManager)}.");

                if (cookie3 != VSConstants.VSCOOKIE_NIL)
                {
                    HResult.Verify(
                        _vsSolutionBuildManager3.UnadviseUpdateSolutionEvents3(cookie3),
                        $"Error unadvising solution events 3 in {typeof(SolutionBuildManager)}.");
                }
            });
        }

        public int QueryBuildManagerBusy()
        {
            Assumes.NotNull(_vsSolutionBuildManager2);

            JoinableFactory.Context.VerifyIsOnMainThread();

            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager2.QueryBuildManagerBusy(out int flags));
            
            return flags;
        }

        public uint QueryBuildManagerBusyEx()
        {
            Assumes.NotNull(_vsSolutionBuildManager3);

            JoinableFactory.Context.VerifyIsOnMainThread();

            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager3.QueryBuildManagerBusyEx(out uint flags));
            
            return flags;
        }

        public void SaveDocumentsBeforeBuild(IVsHierarchy hierarchy, uint itemId, uint docCookie)
        {
            Assumes.NotNull(_vsSolutionBuildManager2);

            JoinableFactory.Context.VerifyIsOnMainThread();

            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager2.SaveDocumentsBeforeBuild(hierarchy, itemId, docCookie));
        }

        public void CalculateProjectDependencies()
        {
            Assumes.NotNull(_vsSolutionBuildManager2);

            JoinableFactory.Context.VerifyIsOnMainThread();

            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager2.CalculateProjectDependencies());
        }

        public IVsHierarchy[] GetProjectDependencies(IVsHierarchy hierarchy)
        {
            Assumes.NotNull(_vsSolutionBuildManager2);

            JoinableFactory.Context.VerifyIsOnMainThread();

            // Find out how many dependent projects there are
            uint[] dependencyCounts = new uint[1];
            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager2.GetProjectDependencies(hierarchy, 0, null, dependencyCounts));
            uint count = dependencyCounts[0];

            if (count == 0)
            {
                return Array.Empty<IVsHierarchy>();
            }

            // Get all of the dependent projects, and add them to our list
            var projectsArray = new IVsHierarchy[count];
            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager2.GetProjectDependencies(hierarchy, count, projectsArray, dependencyCounts));
            return projectsArray;
        }

        public void StartUpdateSpecificProjectConfigurations(IVsHierarchy[] hierarchy, uint[] buildFlags, uint dwFlags)
        {
            Assumes.NotNull(_vsSolutionBuildManager2);

            JoinableFactory.Context.VerifyIsOnMainThread();

            ErrorHandler.ThrowOnFailure(_vsSolutionBuildManager2.StartUpdateSpecificProjectConfigurations(
                cProjs: (uint)hierarchy.Length,
                rgpHier: hierarchy,
                rgpcfg: null,
                rgdwCleanFlags: null,
                rgdwBuildFlags: buildFlags,
                rgdwDeployFlags: null,
                dwFlags: dwFlags,
                fSuppressUI: 0));
        }
    }
}
