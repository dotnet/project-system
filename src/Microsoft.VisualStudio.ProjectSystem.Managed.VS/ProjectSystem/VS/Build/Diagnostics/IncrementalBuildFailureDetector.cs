// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.Build;
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
          IPackageService
    {
        private readonly IVsUIService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;

        private IVsSolutionBuildManager2? _solutionBuildManager;
        private uint _cookie;

        [ImportingConstructor]
        public IncrementalBuildFailureDetector(
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            JoinableTaskContext joinableTaskContext)
            : base(new(joinableTaskContext))
        {
            _featureFlagsService = featureFlagsService;
        }

        async Task IPackageService.InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            await JoinableFactory.SwitchToMainThreadAsync();

            _solutionBuildManager = await asyncServiceProvider.GetServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>();

            HResult.Verify(_solutionBuildManager.AdviseUpdateSolutionEvents(this, out _cookie), $"Error advising solution events in {typeof(IncrementalBuildFailureDetector)}.");

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

            if (_cookie != 0)
            {
                HResult.Verify(_solutionBuildManager.UnadviseUpdateSolutionEvents(_cookie), $"Error unadvising solution events in {typeof(IncrementalBuildFailureDetector)}.");
                _cookie = 0;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called before any build actions have begun. This is the last chance to cancel the build before any building begins.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate) => HResult.OK;

        /// <summary>
        /// Called when a build is completed.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => HResult.OK;

        /// <summary>
        /// Called before the first project configuration is about to be built.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => HResult.OK;

        /// <summary>
        /// Called when a build is being cancelled.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel() => HResult.OK;

        /// <summary>
        /// Called right before a project configuration begins to build.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel) => HResult.OK;

        /// <summary>
        /// Called right after a project configuration is finished building.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            if (fSuccess != 0 && fCancel == 0)
            {
                bool telemetryEnabled = _featureFlagsService.Value.IsFeatureEnabled("ManagedProjectSystem.EnableIncrementalBuildFailureOutputLogging", defaultValue: false);
                bool loggingEnabled = _featureFlagsService.Value.IsFeatureEnabled("ManagedProjectSystem.EnableIncrementalBuildFailureTelemetry", defaultValue: false);

                if (!telemetryEnabled && !loggingEnabled)
                {
                    return HResult.OK;
                }

                UnconfiguredProject? unconfiguredProject = pHierProj.AsUnconfiguredProject();

                if (unconfiguredProject is not null)
                {
                    IProjectChecker? checker = unconfiguredProject.Services.ExportProvider.GetExportedValueOrDefault<IProjectChecker>();
                    IProjectAsynchronousTasksService? projectAsynchronousTasksService = unconfiguredProject.Services.ExportProvider.GetExportedValueOrDefault<IProjectAsynchronousTasksService>(ExportContractNames.Scopes.UnconfiguredProject);

                    if (checker is not null && projectAsynchronousTasksService is not null)
                    {
                        unconfiguredProject.Services.ThreadingPolicy.RunAndForget(
                            async () =>
                            {
                                await TaskScheduler.Default;

                                await checker.CheckAsync(
                                    GetBuildActionFromUpToDateOptions(dwAction),
                                    telemetryEnabled,
                                    loggingEnabled,
                                    projectAsynchronousTasksService.UnloadCancellationToken);
                            },
                            unconfiguredProject: unconfiguredProject);
                    }
                }
            }

            return HResult.OK;

            static BuildAction GetBuildActionFromUpToDateOptions(uint options)
            {
                if ((options & VSConstants.VSUTDCF_PACKAGE) == VSConstants.VSUTDCF_PACKAGE)
                {
                    return BuildAction.Package;
                }
                
                if ((options & VSConstants.VSUTDCF_REBUILD) == VSConstants.VSUTDCF_REBUILD)
                {
                    return BuildAction.Rebuild;
                }

                return BuildAction.Build;
            }
        }

        /// <summary>
        /// Called when the active project configuration for a project in the solution has changed.
        /// </summary>
        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => HResult.OK;

        #region IVsUpdateSolutionEvents stubs

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => HResult.OK;
        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => HResult.OK;

        #endregion
    }
}
