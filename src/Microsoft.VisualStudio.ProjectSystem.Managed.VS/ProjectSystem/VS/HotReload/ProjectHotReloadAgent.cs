// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[Export(typeof(IProjectHotReloadAgent))]
[method: ImportingConstructor]
internal sealed class ProjectHotReloadAgent(
    Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
    Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
    Lazy<IManagedDeltaApplierCreator> managedDeltaApplierCreator) : IProjectHotReloadAgent
{
    public IProjectHotReloadSession CreateHotReloadSession(
        string name,
        int id,
        ConfiguredProject configuredProject,
        IProjectHotReloadSessionCallback callback,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions)
    {
        return new ProjectHotReloadSession(
            name,
            id,
            hotReloadAgentManagerClient: hotReloadAgentManagerClient,
            hotReloadOutputService: hotReloadDiagnosticOutputService,
            deltaApplierCreator: managedDeltaApplierCreator,
            callback: callback,
            buildManager: configuredProject.GetExportedService<IProjectHotReloadBuildManager>(),
            launchProvider: configuredProject.GetExportedService<IProjectHotReloadLaunchProvider>(),
            configuredProject: configuredProject,
            launchProfile: launchProfile,
            debugLaunchOptions: debugLaunchOptions);
    }
}
