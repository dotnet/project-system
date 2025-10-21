// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

/// <summary>
/// Provides information about installed .NET SDKs.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface ISdkInstallationService
{
    /// <summary>
    /// Checks if a specific .NET SDK version is installed on the system.
    /// </summary>
    /// <param name="sdkVersion">The SDK version to check for (e.g., "8.0.415").</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains 
    /// <see langword="true"/> if the SDK version is installed; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> IsSdkInstalledAsync(string sdkVersion);

    /// <summary>
    /// Gets the path to the dotnet.exe executable.
    /// </summary>
    /// <returns>
    /// The full path to dotnet.exe if found; otherwise, <see langword="null"/>.
    /// </returns>
    string? GetDotNetPath();
}
