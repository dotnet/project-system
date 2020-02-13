// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implementation of IRemoteAuthenticationProvider for the "Universal" authentication type.
    /// </summary>
    [Export(typeof(IRemoteAuthenticationProvider))]
    [AppliesTo("SupportUniversalAuthentication")]
    internal class UniversalRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        private static readonly Guid s_universalPortSupplier = new Guid("EE56E4E8-E866-4915-A18E-1DE7114BD7BB");

        [ImportingConstructor]
        public UniversalRemoteAuthenticationProvider()
        {
        }

        public string DisplayName => "Universal";

        public string Name => "None";

        public Guid PortSupplierGuid => VSConstants.DebugPortSupplierGuids.NoAuth_guid;

        public Guid AuthModeGuid => s_universalPortSupplier;

        public uint AdditionalRemoteDiscoveryDialogFlags => /* DEBUG_REMOTE_DISCOVERY_FLAGS2.DRD_SUPPORTS_UNIVERSAL_AUTH */ 2;
    }
}
