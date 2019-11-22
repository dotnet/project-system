// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
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
        private readonly IProjectServiceAccessor _projectServiceAccessor;

        [ImportingConstructor]
        public DotNetCoreProjectCompatibilityDetectorInitializer(IProjectServiceAccessor projectServiceAccessor)
        {
            _projectServiceAccessor = projectServiceAccessor;
        }

        public Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            // Need to use the CPS export provider to get the dotnet compatibility detector
            IDotNetCoreProjectCompatibilityDetector dotNetCoreCompatibilityDetector = _projectServiceAccessor
                .GetProjectService()
                .Services
                .ExportProvider
                .GetExport<IDotNetCoreProjectCompatibilityDetector>()
                .Value;

            return dotNetCoreCompatibilityDetector.InitializeAsync();
        }
    }
}
