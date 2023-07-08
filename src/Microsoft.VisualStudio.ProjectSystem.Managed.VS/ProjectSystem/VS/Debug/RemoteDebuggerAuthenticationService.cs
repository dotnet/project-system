// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        public IRemoteAuthenticationProvider? FindProviderForAuthenticationMode(string remoteAuthenticationMode) => AuthenticationProviders.FirstOrDefaultValue(p => p.Name.Equals(remoteAuthenticationMode, StringComparisons.LaunchProfileProperties));

        public IEnumerable<IRemoteAuthenticationProvider> GetRemoteAuthenticationModes() => AuthenticationProviders.ExtensionValues();

        public bool ShowRemoteDiscoveryDialog(ref string remoteDebugMachine, ref IRemoteAuthenticationProvider? remoteAuthenticationProvider)
        {
            _threadingService.VerifyOnUIThread();

            Guid currentPortSupplier = remoteAuthenticationProvider?.AuthenticationModeGuid ?? Guid.Empty;

            uint extraFlags = (uint)DEBUG_REMOTE_DISCOVERY_FLAGS.DRD_NONE;

            foreach (IRemoteAuthenticationProvider provider in AuthenticationProviders.ExtensionValues())
            {
                extraFlags |= provider.AdditionalRemoteDiscoveryDialogFlags;
            }

            if (ErrorHandler.Succeeded(_remoteDiscoveryUIService.Value.SelectRemoteInstanceViaDlg(remoteDebugMachine, currentPortSupplier, extraFlags, out string remoteMachine, out Guid portSupplier)))
            {
                remoteDebugMachine = remoteMachine;
                remoteAuthenticationProvider = AuthenticationProviders.FirstOrDefaultValue(p => p.AuthenticationModeGuid.Equals(portSupplier));

                return true;
            }

            return false;
        }
    }
}
