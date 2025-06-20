// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

[Export(typeof(IProjectHotReloadAgent))]
internal class ProjectHotReloadAgent : IProjectHotReloadAgent
{
    private readonly Lazy<IHotReloadAgentManagerClient> _hotReloadAgentManagerClient;
    private readonly Lazy<IHotReloadDiagnosticOutputService> _hotReloadDiagnosticOutputService;
    private readonly Lazy<IManagedDeltaApplierCreator> _managedDeltaApplierCreator;
    private readonly IProjectHotReloadBuildManager _buildManager;
    private readonly IProjectHotReloadLaunchProvider _launchProvider;

    [ImportingConstructor]
    public ProjectHotReloadAgent(
        Lazy<IHotReloadAgentManagerClient> hotReloadAgentManagerClient,
        Lazy<IHotReloadDiagnosticOutputService> hotReloadDiagnosticOutputService,
        Lazy<IManagedDeltaApplierCreator> managedDeltaApplierCreator,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadLaunchProvider launchProvider)
    {
        _hotReloadAgentManagerClient = hotReloadAgentManagerClient;
        _hotReloadDiagnosticOutputService = hotReloadDiagnosticOutputService;
        _managedDeltaApplierCreator = managedDeltaApplierCreator;
        _buildManager = buildManager;
        _launchProvider = launchProvider;
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
            _buildManager,
            _launchProvider,
            configuredProject: null);
    }

    public IProjectHotReloadSession? CreateHotReloadSession(string id, string runtimeVersion, IProjectHotReloadSessionCallback callback)
    {
        return CreateHotReloadSession(id, 0, runtimeVersion, callback);
    }
}
