// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractGenerateNuGetPackageCommand : AbstractSingleNodeProjectCommand, IVsUpdateSolutionEvents
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsSolutionBuildManager2 _buildManager;
        private readonly GeneratePackageOnBuildPropertyProvider _generatePackageOnBuildPropertyProvider;

        protected AbstractGenerateNuGetPackageCommand(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            SVsServiceProvider serviceProvider,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(generatePackageOnBuildPropertyProvider, nameof(generatePackageOnBuildPropertyProvider));

            UnconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
            _generatePackageOnBuildPropertyProvider = generatePackageOnBuildPropertyProvider;

            uint cookie;
            _buildManager = serviceProvider.GetService<IVsSolutionBuildManager2, SVsSolutionBuildManager>();
            _buildManager.AdviseUpdateSolutionEvents(this, out cookie);
        }

        protected UnconfiguredProject UnconfiguredProject { get; }

        protected abstract string GetCommandText();
        protected abstract bool ShouldHandle(IProjectTree node);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus) =>
            ShouldHandle(node) ?
                GetCommandStatusResult.Handled(GetCommandText(), IsReadyToBuild() ? CommandStatus.Enabled : CommandStatus.Supported) :
                GetCommandStatusResult.Unhandled;

        private bool IsReadyToBuild()
        {
            return ErrorHandler.Succeeded(_buildManager.QueryBuildManagerBusy(out int busy)) && busy == 0;
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node))
            {
                return false;
            }

            await _threadingService.SwitchToUIThread();

            // Save documents and kick off build.
            var projectVsHierarchy = (IVsHierarchy)UnconfiguredProject.Services.HostObject;
            int hr = _buildManager.SaveDocumentsBeforeBuild(projectVsHierarchy, (uint)VSConstants.VSITEMID.Root, 0 /*docCookie*/);
            if (ErrorHandler.Succeeded(hr))
            {
                // Enable generating package on build ("GeneratePackageOnBuild") for all projects being built.
                _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(true);

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
    }
}
