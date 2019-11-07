// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Packaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Initializes the exported <see cref="IDotNetCoreProjectCompatibilityDetector"/> when the package loads.
    /// </summary>
    [Export(typeof(IPackageService))]
    internal sealed class DotNetCoreProjectCompatibilityDetectorInitializer : IPackageService
    {
        /// <inheritdoc />
        public async Task<IDisposable?> InitializeAsync(ManagedProjectSystemPackage package, IComponentModel componentModel)
        {
            // Need to use the CPS export provider to get the dotnet compatibility detector
            IDotNetCoreProjectCompatibilityDetector dotNetCoreCompatibilityDetector = componentModel
                .GetService<IProjectServiceAccessor>()
                .GetProjectService()
                .Services
                .ExportProvider
                .GetExport<IDotNetCoreProjectCompatibilityDetector>()
                .Value;

            await dotNetCoreCompatibilityDetector.InitializeAsync();

            return null;
        }
    }
}
