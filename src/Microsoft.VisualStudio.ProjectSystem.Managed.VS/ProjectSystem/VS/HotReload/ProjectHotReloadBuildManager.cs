// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[Export(typeof(IProjectHotReloadBuildManager))]
[method: ImportingConstructor]
internal sealed class ProjectHotReloadBuildManager(
    UnconfiguredProject project,
    IProjectThreadingService threadingService,
    IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> solutionBuildManagerService) : IProjectHotReloadBuildManager
{
    private IVsSolutionBuildManager2? _vsSolutionBuildManager2;

    /// <summary>
    /// Build project and wait for the build to complete.
    /// </summary>
    public async Task<bool> BuildProjectAsync(CancellationToken cancellationToken)
    {
        Assumes.NotNull(project.Services.HostObject);
        _vsSolutionBuildManager2 ??= await solutionBuildManagerService.GetValueAsync(cancellationToken);

        if (threadingService.JoinableTaskContext.IsMainThreadBlocked())
        {
            throw new InvalidOperationException("This task cannot be blocked on by the UI thread.");
        }

        // Step 1: Register sbm events
        using var solutionBuildCompleteListener = new SolutionBuildCompleteListener();
        Verify.HResult(_vsSolutionBuildManager2.AdviseUpdateSolutionEvents(solutionBuildCompleteListener, out uint cookie));
        try
        {
            // Step 2: Build
            var projectVsHierarchy = (IVsHierarchy)project.Services.HostObject;

            var result = _vsSolutionBuildManager2.StartSimpleUpdateProjectConfiguration(
                pIVsHierarchyToBuild: projectVsHierarchy,
                pIVsHierarchyDependent: null,
                pszDependentConfigurationCanonicalName: null,
                dwFlags: (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD,
                dwDefQueryResults: (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_SAVEBEFOREBUILD_QUERY_YES,
                fSuppressUI: 0);

            ErrorHandler.ThrowOnFailure(result);

            // Step 3: Wait for the build to complete
            return await solutionBuildCompleteListener.WaitForSolutionBuildCompletedAsync(cancellationToken);
        }
        finally
        {
            _vsSolutionBuildManager2.UnadviseUpdateSolutionEvents(cookie);
        }
    }

    private class SolutionBuildCompleteListener : IVsUpdateSolutionEvents, IDisposable
    {
        private readonly TaskCompletionSource<bool> _buildCompletedSource = new();

        public SolutionBuildCompleteListener()
        {
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            _buildCompletedSource.TrySetResult(fSucceeded != 0);

            return HResult.OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int UpdateSolution_Cancel()
        {
            _buildCompletedSource.TrySetCanceled();

            return HResult.OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return HResult.OK;
        }

        public async Task<bool> WaitForSolutionBuildCompletedAsync(CancellationToken ct = default)
        {
            using var _ = ct.Register(() =>
            {
                _buildCompletedSource.TrySetCanceled();
            });

            try
            {
                return await _buildCompletedSource.Task;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _buildCompletedSource.TrySetCanceled();
        }
    }
}
