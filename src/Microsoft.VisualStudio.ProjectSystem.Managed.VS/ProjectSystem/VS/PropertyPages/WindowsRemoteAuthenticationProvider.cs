// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implementation of IRemoteAuthenticationProvider for the Windows authentication type.
    /// </summary>
    [Export(typeof(IRemoteAuthenticationProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    [Order(2)]
    internal class WindowsRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        private static readonly Guid s_localPortSupplier = new Guid("708C1ECA-FF48-11D2-904F-00C04FA302A1");

        [ImportingConstructor]
        public WindowsRemoteAuthenticationProvider()
        {
        }

        public string DisplayName => Resources.WindowsAuth;

        public string Name => "Windows";

        public Guid PortSupplierGuid => s_localPortSupplier;

        public Guid AuthenticationModeGuid => Guid.Empty;

        public uint AdditionalRemoteDiscoveryDialogFlags => 0;
    }
}
