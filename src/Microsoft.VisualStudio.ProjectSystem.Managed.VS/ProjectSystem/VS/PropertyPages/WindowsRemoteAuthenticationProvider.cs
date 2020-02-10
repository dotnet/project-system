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
    internal class WindowsRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        [ImportingConstructor]
        public WindowsRemoteAuthenticationProvider()
        {
        }

        public string Name => Resources.WindowsAuth;

        public Guid PortSupplier => Guid.Empty;

        public uint AdditionalRemoteDiscoveryDialogFlags => 0;
    }
}
