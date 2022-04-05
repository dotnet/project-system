// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
