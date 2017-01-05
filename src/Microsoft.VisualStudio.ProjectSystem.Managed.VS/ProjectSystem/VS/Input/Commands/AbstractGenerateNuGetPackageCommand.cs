// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractGenerateNuGetPackageCommand : AbstractSingleNodeProjectCommand, IVsUpdateSolutionEvents, IDisposable
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly GeneratePackageOnBuildPropertyProvider _generatePackageOnBuildPropertyProvider;
        private IVsSolutionBuildManager2 _buildManager;
        private uint _solutionEventsCookie;

        protected AbstractGenerateNuGetPackageCommand(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            SVsServiceProvider serviceProvider,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(generatePackageOnBuildPropertyProvider, nameof(generatePackageOnBuildPropertyProvider));

            UnconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
            _serviceProvider = serviceProvider;
            _generatePackageOnBuildPropertyProvider = generatePackageOnBuildPropertyProvider;            
        }

        protected UnconfiguredProject UnconfiguredProject { get; }

        protected abstract string GetCommandText();
        protected abstract bool ShouldHandle(IProjectTree node);

        protected async override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (ShouldHandle(node))
            {
                // Enable the command if the build manager is ready to build.
                var commandStatus = await IsReadyToBuildAsync().ConfigureAwait(false) ? CommandStatus.Enabled : CommandStatus.Supported;
                return await GetCommandStatusResult.Handled(GetCommandText(), commandStatus).ConfigureAwait(false);
            }

            return CommandStatusResult.Unhandled;
        }

        private async Task<bool> IsReadyToBuildAsync()
        {
            // Ensure build manager is initialized.
            await EnsureBuildManagerInitializedAsync().ConfigureAwait(false);

            ErrorHandler.ThrowOnFailure(_buildManager.QueryBuildManagerBusy(out int busy));
            return busy == 0;
        }

        private async Task EnsureBuildManagerInitializedAsync()
        {
            if (_buildManager == null)
            {
                // Switch to UI thread for querying the build manager service.
                await _threadingService.SwitchToUIThread();

                var buildManager = _serviceProvider.GetService<IVsSolutionBuildManager2, SVsSolutionBuildManager>();
                if (Interlocked.CompareExchange(ref _buildManager, buildManager, null) == null)
                {
                    // Register for solution build events.
                    _buildManager.AdviseUpdateSolutionEvents(this, out _solutionEventsCookie);
                }
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
                // Save documents before build.
                var projectVsHierarchy = (IVsHierarchy)UnconfiguredProject.Services.HostObject;
                int hr = _buildManager.SaveDocumentsBeforeBuild(projectVsHierarchy, (uint)VSConstants.VSITEMID.Root, 0 /*docCookie*/);
                ErrorHandler.ThrowOnFailure(hr);

                // Enable generating package on build ("GeneratePackageOnBuild") for all projects being built.
                _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(true);

                // Kick off the build.
                uint dwFlags = (uint)(VSSOLNBUILDUPDATEFLAGS.SBF_SUPPRESS_SAVEBEFOREBUILD_QUERY | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD);
                hr = _buildManager.StartSimpleUpdateProjectConfiguration(projectVsHierarchy, null, null, dwFlags, 0, 0);
                return ErrorHandler.Succeeded(hr);
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
            _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(false);
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(false);
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
            var buildManager = _buildManager;
            if (buildManager != null)
            {
                if (Interlocked.CompareExchange(ref _buildManager, null, buildManager) == buildManager)
                {
                    // Unregister solution build events.
                    buildManager.UnadviseUpdateSolutionEvents(_solutionEventsCookie);
                }
            }
        }
        #endregion
    }
}
