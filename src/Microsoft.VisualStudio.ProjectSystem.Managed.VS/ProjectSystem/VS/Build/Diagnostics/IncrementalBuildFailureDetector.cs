// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    /// <summary>
    ///   Detects when a project's incremental build is not working, and reports a message to the user and publishes telemetry.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     When a project's incremental build is working correctly, it will be up-to-date immediately after build.
    ///   </para>
    ///   <para>
    ///     This component validates that the fast up-to-date check sees the project as up-to-date after build.
    ///   </para>
    ///   <para>
    ///     If not, we:
    ///     <list type="bullet">
    ///       <item>Notify the user and provide a URL for more information.</item>
    ///       <item>Publish telemetry about the failure in order to improve incremental build in future.</item>
    ///     </list>
    ///   </para>
    ///   <para>
    ///     This component lives in the global scope. Its single instance listens to solution build events.
    ///     It then delegates to <see cref="IBuildUpToDateCheckValidator.ValidateUpToDateAsync"/> in the project's
    ///     active configured scope to determine whether incremental build worked.
    ///   </para>
    /// </remarks>
    [Export(typeof(IPackageService))]
    internal sealed partial class IncrementalBuildFailureDetector
        : OnceInitializedOnceDisposedAsync,
          IVsUpdateSolutionEvents2,
          IVsRunningDocTableEvents,
          IPackageService
    {
        private readonly IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> _solutionBuildManagerService;
        private readonly IVsService<SVsRunningDocumentTable, IVsRunningDocumentTable> _rdtService;

        private IVsSolutionBuildManager2? _solutionBuildManager;
        private IVsRunningDocumentTable? _rdt;

        private uint _sbmCookie;
        private uint _rdtCookie;
        
        private DateTime _lastSavedAtUtc = DateTime.MinValue;
        private DateTime _lastBuildStartedAtUtc = DateTime.MinValue;

        [ImportingConstructor]
        public IncrementalBuildFailureDetector(
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> solutionBuildManagerService,
            IVsService<SVsRunningDocumentTable, IVsRunningDocumentTable> rdtService,
            JoinableTaskContext joinableTaskContext)
            : base(new(joinableTaskContext))
        {
            _solutionBuildManagerService = solutionBuildManagerService;
            _rdtService = rdtService;
        }

        async Task IPackageService.InitializeAsync(IAsyncServiceProvider _)
        {
            await JoinableFactory.SwitchToMainThreadAsync();

            _solutionBuildManager = await _solutionBuildManagerService.GetValueAsync();
            _rdt = await _rdtService.GetValueAsync();

            HResult.Verify(_rdt.AdviseRunningDocTableEvents(this, out _rdtCookie), $"Error advising RDT events in {typeof(IncrementalBuildFailureDetector)}.");
            HResult.Verify(_solutionBuildManager.AdviseUpdateSolutionEvents(this, out _sbmCookie), $"Error advising solution events in {typeof(IncrementalBuildFailureDetector)}.");

            await InitializeAsync();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            Assumes.NotNull(_solutionBuildManager);

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            Assumes.NotNull(_solutionBuildManager);
            Assumes.NotNull(_rdt);

            if (_sbmCookie != 0)
            {
                HResult.Verify(_rdt.UnadviseRunningDocTableEvents(_sbmCookie), $"Error unadvising RDT events in {typeof(IncrementalBuildFailureDetector)}.");
                _sbmCookie = 0;
            }

            if (_rdtCookie != 0)
            {
                HResult.Verify(_solutionBuildManager.UnadviseUpdateSolutionEvents(_rdtCookie), $"Error unadvising solution events in {typeof(IncrementalBuildFailureDetector)}.");
                _rdtCookie = 0;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called right after a project configuration is finished building.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            if (fSuccess != 0 && fCancel == 0)
            {
                if (_lastSavedAtUtc > _lastBuildStartedAtUtc)
                {
                    // Something was saved since the last build.
                    // This can cause the project to appear out-of-date in a way that does not indicate
                    // broken incrementality. In such cases, avoid the check here altogether for
                    // this project build.
                    return HResult.OK;
                }

                if (IsRelevantBuild(dwAction))
                {
                    IProjectChecker? checker = pHierProj.AsUnconfiguredProject()?.Services.ExportProvider.GetExportedValueOrDefault<IProjectChecker>();

                    checker?.OnProjectBuildCompleted();
                }
            }

            return HResult.OK;

            static bool IsRelevantBuild(uint options)
            {
                var operation = (VSSOLNBUILDUPDATEFLAGS)options;

                if ((operation & VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD) == VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Called before the first project configuration is about to be built.
        /// </summary>
        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            _lastBuildStartedAtUtc = DateTime.UtcNow;
            return HResult.OK;
        }

        /// <summary>
        /// Called after saving a document in the Running Document Table.
        /// </summary>
        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            _lastSavedAtUtc = DateTime.UtcNow;
            return HResult.OK;
        }

        #region IVsUpdateSolutionEvents stubs

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => HResult.OK;
        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => HResult.OK;

        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel() => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel) => HResult.OK;

        #endregion

        #region IVsRunningDocTableEvents stubs

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => HResult.OK;
        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => HResult.OK;
        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs) => HResult.OK;
        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => HResult.OK;
        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => HResult.OK;

        #endregion
    }
}
