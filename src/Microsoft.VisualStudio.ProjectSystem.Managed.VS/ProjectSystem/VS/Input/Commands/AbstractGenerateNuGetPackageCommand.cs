// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractGenerateNuGetPackageCommand : AbstractSingleNodeProjectCommand, IVsUpdateSolutionEvents, IDisposable
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly ISolutionBuildManager _solutionBuildManager;
        private readonly GeneratePackageOnBuildPropertyProvider _generatePackageOnBuildPropertyProvider;
        
        private IAsyncDisposable? _subscription;

        protected AbstractGenerateNuGetPackageCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            ISolutionBuildManager vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(vsSolutionBuildManagerService, nameof(vsSolutionBuildManagerService));
            Requires.NotNull(generatePackageOnBuildPropertyProvider, nameof(generatePackageOnBuildPropertyProvider));

            Project = project;
            _threadingService = threadingService;
            _solutionBuildManager = vsSolutionBuildManagerService;
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
            // Switch to UI thread for querying the build manager service.
            await _threadingService.SwitchToUIThread();

            // Ensure build manager is initialized.
            _subscription ??= await _solutionBuildManager.SubscribeSolutionEventsAsync(this);

            int busy = _solutionBuildManager.QueryBuildManagerBusy();
            return busy == 0;
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
                _solutionBuildManager.SaveDocumentsBeforeBuild(projectVsHierarchy, (uint)VSConstants.VSITEMID.Root, docCookie: 0);

                // We need to make sure dependencies are built so they can go into the package
                _solutionBuildManager.CalculateProjectDependencies();

                // Assembly our list of projects to build
                var projects = new List<IVsHierarchy>
                {
                    projectVsHierarchy
                };

                projects.AddRange(_solutionBuildManager.GetProjectDependencies(projectVsHierarchy));

                // Turn off "GeneratePackageOnBuild" because otherwise the Pack target will not do a build, even if there is no built output
                _generatePackageOnBuildPropertyProvider.OverrideGeneratePackageOnBuild(false);

                uint dwFlags = (uint)(VSSOLNBUILDUPDATEFLAGS.SBF_SUPPRESS_SAVEBEFOREBUILD_QUERY | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD);

                uint[] buildFlags = new uint[projects.Count];
                // We tell the Solution Build Manager to Package our project, which will call the Pack target, which will build if necessary.
                // Any dependent projects will just do a normal build
                buildFlags[0] = VSConstants.VS_BUILDABLEPROJECTCFGOPTS_PACKAGE;

                _solutionBuildManager.StartUpdateSpecificProjectConfigurations(projects.ToArray(), buildFlags, dwFlags);
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
                if (disposing && _subscription is not null)
                {
                    // Build manager APIs require UI thread access.
                    _threadingService.ExecuteSynchronously(async () =>
                    {
                        await _threadingService.SwitchToUIThread();

                        if (_subscription is not null)
                        {
                            // Unregister solution build events.
                            await _subscription.DisposeAsync();
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
