// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Provides information about the .NET environment and installed SDKs.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IDotNetEnvironment
{
    /// <summary>
    /// Checks if a specific .NET SDK version is installed on the system.
    /// </summary>
    /// <param name="sdkVersion">The SDK version to check for (e.g., "8.0.415").</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains 
    /// <see langword="true"/> if the SDK version is installed; otherwise, <see langword="false"/>.
    /// </returns>
    bool IsSdkInstalled(string sdkVersion);

    /// <summary>
    /// Gets the path to the dotnet.exe executable.
    /// </summary>
    /// <returns>
    /// The full path to dotnet.exe if found; otherwise, <see langword="null"/>.
    /// </returns>
    string? GetDotNetHostPath();

    /// <summary>
    /// Reads the list of installed .NET Core runtimes for the specified architecture from the registry.
    /// </summary>
    /// <remarks>
    /// Returns runtimes installed both as standalone packages, and through VS Setup.
    /// Values have the form <c>3.1.32</c>, <c>7.0.11</c>, <c>8.0.0-preview.7.23375.6</c>, <c>8.0.0-rc.1.23419.4</c>.
    /// If results could not be determined, <see langword="null"/> is returned.
    /// </remarks>
    /// <param name="architecture">The runtime architecture to report results for.</param>
    /// <returns>An array of runtime versions, or <see langword="null"/> if results could not be determined or no runtimes were found.</returns>
    string[]? GetInstalledRuntimeVersions(System.Runtime.InteropServices.Architecture architecture);
}
