// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractRunCodeAnalysisCommand : AbstractSingleNodeProjectCommand, IVsUpdateSolutionEvents, IDisposable
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly RunCodeAnalysisBuildPropertyProvider _runCodeAnalysisBuildPropertyProvider;

        private IVsSolutionBuildManager2 _buildManager;
        private uint _solutionEventsCookie;
        private IVsShell _vsShell;
        private bool? _isCodeAnalysisPackageInstalled;
        private bool _codeAnalysisPackageLoadAttempted;
        private IOleCommandTarget _codeAnalysisCommandTarget;

        protected AbstractRunCodeAnalysisCommand(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            SVsServiceProvider serviceProvider,
            RunCodeAnalysisBuildPropertyProvider runCodeAnalysisBuildPropertyProvider)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(serviceProvider, nameof(runCodeAnalysisBuildPropertyProvider));

            UnconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
            _serviceProvider = serviceProvider;
            _runCodeAnalysisBuildPropertyProvider = runCodeAnalysisBuildPropertyProvider;
        }

        protected UnconfiguredProject UnconfiguredProject { get; }

        protected abstract string GetCommandText();
        protected abstract bool ShouldHandle(IProjectTree node);
        protected abstract long CommandId { get; }

        protected async override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (ShouldHandle(node))
            {
                // Enable the command if code analysis is enabled and the build manager is ready to build.
                if (await IsCodeAnalysisPackageInstalledAsync().ConfigureAwait(false))
                {
                    var commandStatus = await IsReadyToBuildAsync().ConfigureAwait(false) ? CommandStatus.Enabled : CommandStatus.Supported;
                    return await GetCommandStatusResult.Handled(GetCommandText(), commandStatus).ConfigureAwait(false);
                }
             }

            return CommandStatusResult.Unhandled;
        }

        private async Task<bool> IsCodeAnalysisPackageInstalledAsync()
        {
            if (!_isCodeAnalysisPackageInstalled.HasValue)
            {
                // Switch to UI thread for querying the build manager service.
                await _threadingService.SwitchToUIThread();

                if (_vsShell == null)
                {
                    _vsShell = _serviceProvider.GetService<IVsShell, SVsShell>();
                }

                var codeAnalysisGuid = new Guid(ManagedProjectSystemPackage.CodeAnalysisPackageGuid);
                ErrorHandler.ThrowOnFailure(_vsShell.IsPackageInstalled(ref codeAnalysisGuid, out int packageInstalled));
                _isCodeAnalysisPackageInstalled = packageInstalled != 0;
            }

            return _isCodeAnalysisPackageInstalled.Value;
        }

        private async Task EnsureCodeAnalysisPackageLoadedAsync()
        {
            if (!_codeAnalysisPackageLoadAttempted)
            {
                // Switch to UI thread for querying the build manager service.
                await _threadingService.SwitchToUIThread();

                if (_vsShell == null)
                {
                    _vsShell = _serviceProvider.GetService<IVsShell, SVsShell>();
                }

                var codeAnalysisGuid = new Guid(ManagedProjectSystemPackage.CodeAnalysisPackageGuid);
                _vsShell.IsPackageLoaded(ref codeAnalysisGuid, out IVsPackage package);
                if (package == null)
                {
                    _vsShell.LoadPackage(ref codeAnalysisGuid, out package);
                }

                _codeAnalysisCommandTarget = package as IOleCommandTarget;
                _codeAnalysisPackageLoadAttempted = true;
            }
        }

        private async Task<bool> IsReadyToBuildAsync()
        {
            // Ensure build manager is initialized.
            await EnsureBuildManagerInitializedAsync().ConfigureAwait(true);

            ErrorHandler.ThrowOnFailure(_buildManager.QueryBuildManagerBusy(out int busy));
            return busy == 0;
        }

        private async Task EnsureBuildManagerInitializedAsync()
        {
            // Switch to UI thread for querying the build manager service.
            await _threadingService.SwitchToUIThread();

            if (_buildManager == null)
            {
                _buildManager = _serviceProvider.GetService<IVsSolutionBuildManager2, SVsSolutionBuildManager>();

                // Register for solution build events.
                _buildManager.AdviseUpdateSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node))
            {
                return false;
            }

            if (await IsReadyToBuildAsync().ConfigureAwait(false))
            {
                // Build manager APIs require UI thread access.
                await _threadingService.SwitchToUIThread();

                await EnsureCodeAnalysisPackageLoadedAsync().ConfigureAwait(false);

                // Enable RunCodeAnalysisOnce on this project.
                _runCodeAnalysisBuildPropertyProvider.EnableRunCodeAnalysisOnBuild(UnconfiguredProject.FullPath);

                _codeAnalysisCommandTarget?.Exec(VSConstants.VSStd2K, (uint)CommandId, (uint)commandExecuteOptions, variantArgIn, variantArgOut);
            }

            return true;
        }

        #region IVsUpdateSolutionEvents members
        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            _runCodeAnalysisBuildPropertyProvider.DisableRunCodeAnalysisOnBuild();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            _runCodeAnalysisBuildPropertyProvider.DisableRunCodeAnalysisOnBuild();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            // Build manager APIs require UI thread access.
            _threadingService.ExecuteSynchronously(async () =>
            {
                await _threadingService.SwitchToUIThread();

                if (_buildManager != null)
                {
                    // Unregister solution build events.
                    _buildManager.UnadviseUpdateSolutionEvents(_solutionEventsCookie);
                    _buildManager = null;
                }
            });
        }
        #endregion
    }
}
