// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

[Export(typeof(IProjectHotReloadAgent))]
[method: ImportingConstructor]
internal sealed class ProjectHotReloadAgent(
    Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
    Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
    [Import(AllowDefault = true)] IHotReloadDebugStateProvider? debugStateProvider) // allow default until VS Code is updated: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2571211
        : IProjectHotReloadAgent 
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
            callback: callback,
            buildManager: configuredProject.GetExportedService<IProjectHotReloadBuildManager>(),
            launchProvider: configuredProject.GetExportedService<IProjectHotReloadLaunchProvider>(),
            configuredProject: configuredProject,
            launchProfile: launchProfile,
            debugLaunchOptions: debugLaunchOptions,
            debugStateProvider ?? DefaultDebugStateProvider.Instance);
    }

    private sealed class DefaultDebugStateProvider : IHotReloadDebugStateProvider
    {
        public static readonly DefaultDebugStateProvider Instance = new();

        public ValueTask<bool> IsSuspendedAsync(CancellationToken cancellationToken)
            => new(false);
    }
}
