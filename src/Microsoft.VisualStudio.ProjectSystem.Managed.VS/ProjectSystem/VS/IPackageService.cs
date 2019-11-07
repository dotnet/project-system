// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Packaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// A service that is initialized when the VS package is initialized.
    /// </summary>
    /// <remarks>
    /// Implementations must be exported in global scope.
    /// </remarks>
    internal interface IPackageService
    {
        /// <summary>
        /// Called when the package is initializing.
        /// </summary>
        /// <remarks>
        /// Must be called from the UI thread.
        /// </remarks>
        /// <param name="package">The VS package.</param>
        /// <param name="componentModel">The VS package.</param>
        /// <returns>An optional disposable object, to be disposed when the package is disposed.</returns>
        Task<IDisposable?> InitializeAsync(ManagedProjectSystemPackage package, IComponentModel componentModel);
    }
}
