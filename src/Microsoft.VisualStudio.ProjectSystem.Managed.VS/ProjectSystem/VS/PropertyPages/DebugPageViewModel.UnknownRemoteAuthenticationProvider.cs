// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal partial class DebugPageViewModel
    {
        private class UnknownRemoteAuthenticationProvider : IRemoteAuthenticationProvider
        {
            private Guid _portSupplier;

            public UnknownRemoteAuthenticationProvider(Guid portSupplier)
            {
                _portSupplier = portSupplier;
            }

            public string Name => $"Unknown - {_portSupplier:D}"; 

            public Guid PortSupplier => _portSupplier;

            public uint AdditionalRemoteDiscoveryDialogFlags => 0;
        }
    }
}
