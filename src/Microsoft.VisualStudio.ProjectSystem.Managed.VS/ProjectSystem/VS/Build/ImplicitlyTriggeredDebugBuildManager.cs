// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Tracks implicitly builds triggered as part of F5/Ctrl+F5 debug/launch commands and
    /// updates the <see cref="IImplicitlyTriggeredBuildManager"/> appropriately.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ImplicitlyTriggeredDebugBuildManager : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent, IVsUpdateSolutionEvents2, IVsUpdateSolutionEvents3
    {
        private readonly IStartupProjectHelper _startupProjectHelper;
        private readonly ISolutionBuildManager _solutionBuildManager;

#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
        private readonly IImplicitlyTriggeredBuildManager _implicitlyTriggeredBuildManager;
#pragma warning restore CS0618 // Type or member is obsolete

        private IAsyncDisposable? _solutionBuildEventsSubscription;

        [ImportingConstructor]
        public ImplicitlyTriggeredDebugBuildManager(
            IProjectThreadingService threadingService,
            ISolutionBuildManager solutionBuildManager,
#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
            IImplicitlyTriggeredBuildManager implicitlyTriggeredBuildManager,
#pragma warning restore CS0618 // Type or member is obsolete
            IStartupProjectHelper startupProjectHelper)
            : base(threadingService.JoinableTaskContext)
        {
            _solutionBuildManager = solutionBuildManager;
            _implicitlyTriggeredBuildManager = implicitlyTriggeredBuildManager;
            _startupProjectHelper = startupProjectHelper;
        }

        public Task LoadAsync() => InitializeAsync();
        public Task UnloadAsync() => DisposeAsync();

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _solutionBuildEventsSubscription = await _solutionBuildManager.SubscribeSolutionEventsAsync(this);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                if (_solutionBuildEventsSubscription is not null)
                {
                    return _solutionBuildEventsSubscription.DisposeAsync().AsTask();
                }
            }

            return Task.CompletedTask;
        }

        private bool IsImplicitlyTriggeredBuild()
        {
            Assumes.NotNull(_solutionBuildManager);

            var buildFlags = (VSSOLNBUILDUPDATEFLAGS)_solutionBuildManager.QueryBuildManagerBusyEx();

            return (buildFlags & (VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCH | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCHDEBUG)) != 0;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            if (IsImplicitlyTriggeredBuild())
            {
                if (_implicitlyTriggeredBuildManager is IImplicitlyTriggeredBuildManager2 implicitlyTriggeredBuildManager2)
                {
                    ImmutableArray<string> startupProjectFullPaths = _startupProjectHelper.GetFullPathsOfStartupProjects();
                    implicitlyTriggeredBuildManager2.OnBuildStart(startupProjectFullPaths);
                }
                else
                {
                    _implicitlyTriggeredBuildManager.OnBuildStart();
                }
            }

            return HResult.OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            if (IsImplicitlyTriggeredBuild())
            {
                _implicitlyTriggeredBuildManager.OnBuildEndOrCancel();
            }

            return HResult.OK;
        }

        public int UpdateSolution_Cancel()
        {
            if (IsImplicitlyTriggeredBuild())
            {
                _implicitlyTriggeredBuildManager.OnBuildEndOrCancel();
            }

            return HResult.OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return HResult.OK;
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return HResult.OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return HResult.OK;
        }

        public int OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return HResult.OK;
        }

        public int OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return HResult.OK;
        }
    }
}
