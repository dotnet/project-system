// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

/// <summary>
///   A service to print out diagnostic messages about project system functionality.
///   Meant to be used in place of the IManagedProjectDiagnosticOutputService from CPS.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.OneOrZero)]
internal interface IManagedProjectDiagnosticOutputService
{
    /// <summary>
    ///    Gets a value indicating whether the output service is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///   Writes a line of message to the output service.
    /// </summary>
    /// <param name="outputMessage">The message string.</param>
    void WriteLine(string outputMessage);
}
