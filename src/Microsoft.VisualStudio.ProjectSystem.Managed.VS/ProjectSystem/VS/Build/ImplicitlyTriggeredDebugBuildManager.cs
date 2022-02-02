// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

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
        private readonly IVsService<IVsSolutionBuildManager3> _solutionBuildManagerService;
        private readonly IStartupProjectHelper _startupProjectHelper;

#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
        private readonly IImplicitlyTriggeredBuildManager _implicitlyTriggeredBuildManager;
#pragma warning restore CS0618 // Type or member is obsolete

        private IVsSolutionBuildManager3? _solutionBuildManager;
        private uint _cookie, _cookie3;

        [ImportingConstructor]
        public ImplicitlyTriggeredDebugBuildManager(
            IProjectThreadingService threadingService,
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager3> solutionBuildManagerService,
#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
            IImplicitlyTriggeredBuildManager implicitlyTriggeredBuildManager,
#pragma warning restore CS0618 // Type or member is obsolete
            IStartupProjectHelper startupProjectHelper)
            : base(threadingService.JoinableTaskContext)
        {
            _solutionBuildManagerService = solutionBuildManagerService;
            _implicitlyTriggeredBuildManager = implicitlyTriggeredBuildManager;
            _startupProjectHelper = startupProjectHelper;
        }

        public Task LoadAsync() => InitializeAsync();
        public Task UnloadAsync() => DisposeAsync();

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // AdviseUpdateSolutionEvents call needs UI thread.
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            _solutionBuildManager = await _solutionBuildManagerService.GetValueAsync(cancellationToken);
            (_solutionBuildManager as IVsSolutionBuildManager2)?.AdviseUpdateSolutionEvents(this, out _cookie);
            ErrorHandler.ThrowOnFailure(_solutionBuildManager.AdviseUpdateSolutionEvents3(this, out _cookie3));
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                (_solutionBuildManager as IVsSolutionBuildManager2)?.UnadviseUpdateSolutionEvents(_cookie);
                _solutionBuildManager!.UnadviseUpdateSolutionEvents3(_cookie3);
            }

            return Task.CompletedTask;
        }

        private bool IsImplicitlyTriggeredBuild()
        {
            ErrorHandler.ThrowOnFailure(_solutionBuildManager!.QueryBuildManagerBusyEx(out uint flags));
            var buildFlags = (VSSOLNBUILDUPDATEFLAGS)flags;
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
