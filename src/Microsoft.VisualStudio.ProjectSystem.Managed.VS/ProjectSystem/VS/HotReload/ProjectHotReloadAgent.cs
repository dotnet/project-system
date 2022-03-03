// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    [Export(typeof(IProjectHotReloadAgent))]
    internal class ProjectHotReloadAgent : IProjectHotReloadAgent
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

        public IProjectHotReloadSession? CreateHotReloadSession(string id, string runtimeVersion, IProjectHotReloadSessionCallback callback)
        {
            return new ProjectHotReloadSession(
                id,
                runtimeVersion,
                _hotReloadAgentManagerClient,
                _hotReloadDiagnosticOutputService,
                _managedDeltaApplierCreator,
                callback);
        }
    }
}
