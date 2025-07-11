﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

[Export(typeof(IProjectHotReloadAgent2))]
[Export(typeof(IProjectHotReloadAgent))]
internal class ProjectHotReloadAgent : IProjectHotReloadAgent2
{
    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;
    private readonly Lazy<IManagedDeltaApplierCreator> _managedDeltaApplierCreator;

    [ImportingConstructor]
    public ProjectHotReloadAgent(
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
        Lazy<IManagedDeltaApplierCreator> managedDeltaApplierCreator)
    {
        _hotReloadAgentManagerClient = hotReloadAgentManagerClient;
        _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        _managedDeltaApplierCreator = managedDeltaApplierCreator;
    }

    public IProjectHotReloadSession? CreateHotReloadSession(string id, int variant, string runtimeVersion, IProjectHotReloadSessionCallback callback)
    {
        return new ProjectHotReloadSession(
            id,
            variant,
            runtimeVersion,
            _hotReloadAgentManagerClient,
            _hotReloadDiagnosticOutputService,
            _managedDeltaApplierCreator,
            callback,
            buildManager: null,
            launchProvider: null,
            configuredProject: null);
    }

    public IProjectHotReloadSession? CreateHotReloadSession(string id, string runtimeVersion, IProjectHotReloadSessionCallback callback)
    {
        return CreateHotReloadSession(id, 0, runtimeVersion, callback);
    }

    public IProjectHotReloadSession CreateHotReloadSession(
        string id,
        int variant,
        string runtimeVersion,
        ConfiguredProject configuredProject,
        IProjectHotReloadLaunchProvider launchProvider,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadSessionCallback callback,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions)
    {
        return new ProjectHotReloadSession(
            name: id,
            variant: variant,
            runtimeVersion: runtimeVersion,
            hotReloadAgentManagerClient: _hotReloadAgentManagerClient,
            hotReloadOutputService: _hotReloadDiagnosticOutputService,
            deltaApplierCreator: _managedDeltaApplierCreator,
            callback: callback,
            buildManager: buildManager,
            launchProvider: launchProvider,
            configuredProject: configuredProject,
            launchProfile: launchProfile,
            debugLaunchOptions: debugLaunchOptions);
    }
}
