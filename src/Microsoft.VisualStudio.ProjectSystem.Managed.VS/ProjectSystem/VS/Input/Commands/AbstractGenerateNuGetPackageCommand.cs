// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractGenerateNuGetPackageCommand : AbstractSingleNodeProjectCommand, IVsUpdateSolutionEvents, IDisposable
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<IVsSolutionBuildManager2> _vsSolutionBuildManagerService;
        private readonly GeneratePackageOnBuildPropertyProvider _generatePackageOnBuildPropertyProvider;
        private IVsSolutionBuildManager2? _buildManager;
        private uint _solutionEventsCookie;

        protected AbstractGenerateNuGetPackageCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(vsSolutionBuildManagerService, nameof(vsSolutionBuildManagerService));
            Requires.NotNull(generatePackageOnBuildPropertyProvider, nameof(generatePackageOnBuildPropertyProvider));

            Project = project;
            _threadingService = threadingService;
            _vsSolutionBuildManagerService = vsSolutionBuildManagerService;
            _generatePackageOnBuildPropertyProvider = generatePackageOnBuildPropertyProvider;
        }

        protected UnconfiguredProject Project { get; }

        protected abstract string GetCommandText();

        protected abstract bool ShouldHandle(IProjectTree node);

        protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (ShouldHandle(node))
            {
                // Enable the command if the build manager is ready to build.
                CommandStatus commandStatus = await IsReadyToBuildAsync() ? CommandStatus.Enabled : CommandStatus.Supported;
                return await GetCommandStatusResult.Handled(GetCommandText(), commandStatus);
            }

            return CommandStatusResult.Unhandled;
        }

        private async Task<bool> IsReadyToBuildAsync()
        {
            // Ensure build manager is initialized.
            await EnsureBuildManagerInitializedAsync();

            ErrorHandler.ThrowOnFailure(_buildManager!.QueryBuildManagerBusy(out int busy));
            return busy == 0;
        }

        private async Task EnsureBuildManagerInitializedAsync()
        {
            // Switch to UI thread for querying the build manager service.
            await _threadingService.SwitchToUIThread();

            if (_buildManager == null)
            {
                _buildManager = await _vsSolutionBuildManagerService.GetValueAsync();

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

            if (await IsReadyToBuildAsync())
            {
                // Build manager APIs require UI thread access.
                await _threadingService.SwitchToUIThread();

                Assumes.NotNull(Project.Services.HostObject);

                // Save documents before build.
                var projectVsHierarchy = (IVsHierarchy)Project.Services.HostObject;
                ErrorHandler.ThrowOnFailure(_buildManager!.SaveDocumentsBeforeBuild(projectVsHierarchy, (uint)VSConstants.VSITEMID.Root, docCookie: 0));

                // We need to make sure dependencies are built so they can go into the package
                ErrorHandler.ThrowOnFailure(_buildManager.CalculateProjectDependencies());

                // Assembly our list of projects to build
                var projects = new List<IVsHierarchy>
                {
                    projectVsHierarchy
                };

                // First we find out how many dependent projects there are
                uint[] dependencyCounts = new uint[1];
                ErrorHandler.ThrowOnFailure(_buildManager.GetProjectDependencies(projectVsHierarchy, 0, null, dependencyCounts));

                if (dependencyCounts[0] > 0)
                {
                    // Get all of the dependent projects, and add them to our list
                    var projectsArray = new IVsHierarchy[dependencyCounts[0]];
                    ErrorHandler.ThrowOnFailure(_buildManager.GetProjectDependencies(projectVsHierarchy, dependencyCounts[0], projectsArray, dependencyCounts));
                    projects.AddRange(projectsArray);
                }

                // Turn off "GeneratePackageOnBuild" because otherwise the Pack target will not do a build, even if there is no built output
                _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(false);

                uint dwFlags = (uint)(VSSOLNBUILDUPDATEFLAGS.SBF_SUPPRESS_SAVEBEFOREBUILD_QUERY | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD);

                uint[] buildFlags = new uint[projects.Count];
                // We tell the Solution Build Manager to Package our project, which will call the Pack target, which will build if necessary.
                // Any dependent projects will just do a normal build
                buildFlags[0] = VSConstants.VS_BUILDABLEPROJECTCFGOPTS_PACKAGE;

                ErrorHandler.ThrowOnFailure(_buildManager.StartUpdateSpecificProjectConfigurations(cProjs: (uint)projects.Count,
                                                                                                   rgpHier: projects.ToArray(),
                                                                                                   rgpcfg: null,
                                                                                                   rgdwCleanFlags: null,
                                                                                                   rgdwBuildFlags: buildFlags,
                                                                                                   rgdwDeployFlags: null,
                                                                                                   dwFlags: dwFlags,
                                                                                                   fSuppressUI: 0));
            }

            return true;
        }

        #region IVsUpdateSolutionEvents members
        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(null);
            return HResult.OK;
        }

        public int UpdateSolution_Cancel()
        {
            _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(null);
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
        #endregion

        #region IDisposable
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _buildManager != null)
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

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
