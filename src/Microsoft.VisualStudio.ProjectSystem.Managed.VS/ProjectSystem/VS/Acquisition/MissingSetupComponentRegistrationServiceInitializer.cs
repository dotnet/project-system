// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition;

/// <summary>
/// Initializes the exported <see cref="IMissingSetupComponentRegistrationService"/> when the package loads.
/// </summary>
[Export(typeof(IPackageService))]
internal sealed class MissingSetupComponentRegistrationServiceInitializer : IPackageService
{
    private readonly IProjectServiceAccessor _projectServiceAccessor;

    [ImportingConstructor]
    public MissingSetupComponentRegistrationServiceInitializer(IProjectServiceAccessor projectServiceAccessor)
    {
        _projectServiceAccessor = projectServiceAccessor;
    }

    public Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
    {
        IMissingSetupComponentRegistrationService missingWorkloadRegistrationService = _projectServiceAccessor
            .GetProjectService()
            .Services
            .ExportProvider
            .GetExport<IMissingSetupComponentRegistrationService>()
            .Value;

        return missingWorkloadRegistrationService.InitializeAsync();
    }
}
