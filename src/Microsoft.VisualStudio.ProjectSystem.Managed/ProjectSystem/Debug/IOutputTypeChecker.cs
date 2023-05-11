// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
/// A helper to simplify checking if a project produces a library or executable.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IOutputTypeChecker
{
    /// <summary>
    /// Returns <see langword="true"/> if the project produces a library (e.g. a .dll), and <see langword="false"/>
    /// otherwise.
    /// </summary>
    Task<bool> IsConsoleAsync();

    /// <summary>
    /// Returns <see langword="true"/> if the project produces an executable (e.g. a .exe), and <see langword="false"/>
    /// otherwise.
    /// </summary>
    Task<bool> IsLibraryAsync();
}
