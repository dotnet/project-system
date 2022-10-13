// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging;

#pragma warning disable RS0030 // Do not used banned APIs
/// <summary>
///   An implementation of the <see cref="IManagedProjectDiagnosticOutputService"/> that
///   delegates to the CPS <see cref="IProjectDiagnosticOutputService"/>.
/// </summary>
/// <remarks>
///   Note <see cref="IProjectDiagnosticOutputService"/> has been banned in order to
///   encourage the use of the more widely available <see cref="IManagedProjectDiagnosticOutputService"/>,
///   not for any technical reason.
/// </remarks>
[Export(typeof(IManagedProjectDiagnosticOutputService))]
#pragma warning restore RS0030 // Do not used banned APIs
[AppliesTo(ProjectCapability.DotNet)]
internal class VsManagedProjectDiagnosticOutputService : IManagedProjectDiagnosticOutputService
{
    private readonly IProjectDiagnosticOutputService _projectDiagnosticOutputService;

    [ImportingConstructor]
    public VsManagedProjectDiagnosticOutputService(IProjectDiagnosticOutputService projectDiagnosticOutputService)
    {
        _projectDiagnosticOutputService = projectDiagnosticOutputService;
    }

#pragma warning disable RS0030 // Do not used banned APIs
    public bool IsEnabled => _projectDiagnosticOutputService.IsEnabled;
#pragma warning restore RS0030 // Do not used banned APIs

    public void WriteLine(string outputMessage)
    {
#pragma warning disable RS0030 // Do not used banned APIs
        _projectDiagnosticOutputService.WriteLine(outputMessage);
#pragma warning restore RS0030 // Do not used banned APIs
    }
}
