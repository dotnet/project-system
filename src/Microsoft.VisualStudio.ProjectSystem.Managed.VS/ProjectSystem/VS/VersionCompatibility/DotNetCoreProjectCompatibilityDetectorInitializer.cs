// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Initializes the exported <see cref="IDotNetCoreProjectCompatibilityDetector"/> when the package loads.
    /// </summary>
    [Export(typeof(IPackageService))]
    internal sealed class DotNetCoreProjectCompatibilityDetectorInitializer : IPackageService
    {
        public async Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            IComponentModel componentModel = await asyncServiceProvider.GetServiceAsync<SComponentModel, IComponentModel>();

            // Need to use the CPS export provider to get the dotnet compatibility detector
            IDotNetCoreProjectCompatibilityDetector dotNetCoreCompatibilityDetector = componentModel
                .GetService<IProjectServiceAccessor>()
                .GetProjectService()
                .Services
                .ExportProvider
                .GetExport<IDotNetCoreProjectCompatibilityDetector>()
                .Value;

            await dotNetCoreCompatibilityDetector.InitializeAsync();
        }
    }
}
