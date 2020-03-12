// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
