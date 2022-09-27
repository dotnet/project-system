// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implementation of IRemoteAuthenticationProvider for the "Universal" authentication type.
    /// </summary>
    [Export(typeof(IRemoteAuthenticationProvider))]
    [AppliesTo(ProjectCapability.SupportUniversalAuthentication)]
    [Order(30)]
    internal class UniversalRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        private static readonly Guid s_universalPortSupplier = new("EE56E4E8-E866-4915-A18E-1DE7114BD7BB");

        [ImportingConstructor]
        public UniversalRemoteAuthenticationProvider()
        {
        }

        public string DisplayName => "Universal"; // TODO: Does this need to be localized?

        public string Name => "Universal";

        public Guid PortSupplierGuid => VSConstants.DebugPortSupplierGuids.NoAuth_guid;

        public Guid AuthenticationModeGuid => s_universalPortSupplier;

        public uint AdditionalRemoteDiscoveryDialogFlags => /* DEBUG_REMOTE_DISCOVERY_FLAGS2.DRD_SUPPORTS_UNIVERSAL_AUTH */ 2;
    }
}
