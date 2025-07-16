// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[Export(typeof(IProjectHotReloadBuildManager))]
[method: ImportingConstructor]
internal sealed class ProjectHotReloadBuildManager(
    UnconfiguredProject project,
    ISolutionBuildManager solutionBuildManagerService,
    IProjectThreadingService threadingService,
    Lazy<IHotReloadDiagnosticOutputService> hotReloadLogger) : IProjectHotReloadBuildManager
{
    /// <summary>
    /// Build project and wait for the build to complete.
    /// </summary>
    public async Task<bool> BuildProjectAsync(CancellationToken cancellationToken)
    {
        try
        {
            Assumes.NotNull(project.Services.HostObject);
            await threadingService.SwitchToUIThread(cancellationToken);

            return await solutionBuildManagerService.BuildProjectAndWaitForCompletionAsync(
           (IVsHierarchy)project.Services.HostObject,
           cancellationToken);
        }
        catch(Exception ex)
        {
            hotReloadLogger.Value.WriteLine(
                new HotReloadLogMessage(
                    HotReloadVerbosity.Detailed,
                    ex.Message + Environment.NewLine + ex.StackTrace,
                    null,
                    null,
                    0,
                    HotReloadDiagnosticErrorLevel.Error),

                cancellationToken);

            throw;
        }
    }
}
