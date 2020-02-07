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
    internal class NoAuthRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        [ImportingConstructor]
        public NoAuthRemoteAuthenticationProvider(UnconfiguredProject _) // force MEF scope
        {
        }

        public string Name => Resources.NoAuth;

        public Guid PortSupplier => VSConstants.DebugPortSupplierGuids.NoAuth_guid;
    }
}
