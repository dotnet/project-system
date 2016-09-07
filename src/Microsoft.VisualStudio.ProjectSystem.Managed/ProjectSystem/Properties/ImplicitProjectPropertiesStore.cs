// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This class provides a store for the ImplicitProjectPropertiesProvider that sits at the
    /// UnconfiguredProject scope. This allows implicit properties such as the Project Guid to
    /// be shared by all configured projects, each of which will get its own <see cref="ImplicitProjectPropertiesProvider"/>.
    /// </summary>
    [Export(typeof(ImplicitProjectPropertiesStore<,>))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ImplicitProjectPropertiesStore<T1, T2> : ConcurrentDictionary<T1, T2>
    {
        // We import UnconfiguredProject here to ensure that we're loaded into the UnconfiguredProject scope.
        // However, we don't need the project for anything.
        [ImportingConstructor]
        public ImplicitProjectPropertiesStore(UnconfiguredProject project) { }
    }
}
