// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

/// <summary>
/// Provides access to .NET releases information
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private)]
internal interface IDotNetReleasesProvider
{
    /// <summary>
    /// Returns a newer supported .NET SDK version within the same major/minor band as <paramref name="sdkVersion"/>.
    /// </summary>
    /// <param name="sdkVersion">The SDK version to check for support or to use as a baseline for finding the latest version.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<string?> GetNewerSupportedSdkVersionAsync(string sdkVersion, CancellationToken cancellationToken = default);
}
