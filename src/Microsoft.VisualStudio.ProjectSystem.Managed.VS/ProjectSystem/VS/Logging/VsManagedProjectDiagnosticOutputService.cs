// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging;

/// <summary>
///   An implementation of the <see cref="IManagedProjectDiagnosticOutputService"/> that
///   delegates to the CPS <see cref="IProjectDiagnosticOutputService"/>.
/// </summary>
[Export(typeof(IManagedProjectDiagnosticOutputService))]
[AppliesTo(ProjectCapability.DotNet)]
internal class VsManagedProjectDiagnosticOutputService : IManagedProjectDiagnosticOutputService
{
    private readonly IProjectDiagnosticOutputService _projectDiagnosticOutputService;

    [ImportingConstructor]
    public VsManagedProjectDiagnosticOutputService(IProjectDiagnosticOutputService projectDiagnosticOutputService)
    {
        _projectDiagnosticOutputService = projectDiagnosticOutputService;
    }

    public bool IsEnabled => _projectDiagnosticOutputService.IsEnabled;

    public void WriteLine(string outputMessage)
    {
        _projectDiagnosticOutputService.WriteLine(outputMessage);
    }
}
