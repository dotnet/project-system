// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implementation of IRemoteAuthenticationProvider for the "No Auth" authentication type.
    /// </summary>
    [Export(typeof(IRemoteAuthenticationProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    [Order(10)]
    internal class NoAuthRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        [ImportingConstructor]
        public NoAuthRemoteAuthenticationProvider()
        {
        }

        public string DisplayName => Resources.NoAuth;

        public string Name => "None";

        public Guid PortSupplierGuid => VSConstants.DebugPortSupplierGuids.NoAuth_guid;

        public Guid AuthenticationModeGuid => VSConstants.DebugPortSupplierGuids.NoAuth_guid;

        public uint AdditionalRemoteDiscoveryDialogFlags => 0;
    }
}
