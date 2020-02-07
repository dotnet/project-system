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
    [AppliesTo("WinUI")] // TODO: ???
    internal class UniversalRemoteAuthenticationProvider : IRemoteAuthenticationProvider
    {
        [ImportingConstructor]
        public UniversalRemoteAuthenticationProvider(UnconfiguredProject _) // force MEF scope
        {
        }

        public string Name => "Universal";

        public Guid PortSupplier => new Guid("EE56E4E8-E866-4915-A18E-1DE7114BD7BB");
    }
}
