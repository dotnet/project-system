// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [Export(typeof(IRemoteDebuggerAuthenticationService))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class RemoteDebuggerAuthenticationService : IRemoteDebuggerAuthenticationService
    {
        private readonly IVsUIService<SVsDebugRemoteDiscoveryUI, IVsDebugRemoteDiscoveryUI> _remoteDiscoveryUIService;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public RemoteDebuggerAuthenticationService(
            UnconfiguredProject unconfiguredProject,
            IVsUIService<SVsDebugRemoteDiscoveryUI, IVsDebugRemoteDiscoveryUI> remoteDiscoveryUIService,
            IProjectThreadingService threadingService)
        {
            _remoteDiscoveryUIService = remoteDiscoveryUIService;
            _threadingService = threadingService;
            AuthenticationProviders = new OrderPrecedenceImportCollection<IRemoteAuthenticationProvider>(orderingStyle: ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast, projectCapabilityCheckProvider: unconfiguredProject);
        }

        [ImportMany]
        private OrderPrecedenceImportCollection<IRemoteAuthenticationProvider> AuthenticationProviders { get; }

        public IRemoteAuthenticationProvider? GetProviderForAuthenticationMode(string? remoteAuthenticationMode) => AuthenticationProviders.FirstOrDefaultValue(p => p.Name.Equals(remoteAuthenticationMode, StringComparisons.LaunchProfileProperties));

        public IEnumerable<IRemoteAuthenticationProvider> GetRemoteAuthenticationModes() => AuthenticationProviders.ExtensionValues();

        public bool ShowRemoteDiscoveryDialog(ref string remoteDebugMachine, ref IRemoteAuthenticationProvider? remoteAuthenticationProvider)
        {
            _threadingService.VerifyOnUIThread();

            Guid currentPortSupplier = remoteAuthenticationProvider?.AuthModeGuid ?? Guid.Empty;

            uint extraFlags = (uint)DEBUG_REMOTE_DISCOVERY_FLAGS.DRD_NONE;

            foreach (IRemoteAuthenticationProvider provider in AuthenticationProviders.ExtensionValues())
            {
                extraFlags |= provider.AdditionalRemoteDiscoveryDialogFlags;
            }

            IVsDebugRemoteDiscoveryUI? remoteDiscoveryUIService = _remoteDiscoveryUIService.Value;
            Assumes.Present(remoteDiscoveryUIService);

            if (ErrorHandler.Succeeded(remoteDiscoveryUIService.SelectRemoteInstanceViaDlg(remoteDebugMachine, currentPortSupplier, extraFlags, out string remoteMachine, out Guid portSupplier)))
            {
                remoteDebugMachine = remoteMachine;
                remoteAuthenticationProvider = AuthenticationProviders.FirstOrDefaultValue(p => p.AuthModeGuid.Equals(portSupplier));

                return true;
            }

            return false;
        }
    }
}
