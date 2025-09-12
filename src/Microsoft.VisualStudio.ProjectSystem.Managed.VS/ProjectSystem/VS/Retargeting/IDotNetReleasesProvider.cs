// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

/// <summary>
/// Provides access to .NET releases information
/// </summary>
internal interface IDotNetReleasesProvider
{
    /// <summary>
    /// Returns the supported or latest .NET SDK version based on the specified <paramref name="sdkVersion"/>.
    /// </summary>
    /// <param name="sdkVersion">The SDK version to check for support or to use as a baseline for finding the latest version. If null, just get the latest</param>
    /// <param name="includePreview">If true, preview SDK versions may be included in the result; otherwise, only stable versions are considered. Default is false.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the supported or latest SDK version as a string, or null if no suitable version is found.
    /// </returns>
    Task<string?> GetSupportedOrLatestSdkVersionAsync(
        string? sdkVersion,
        bool includePreview = false,
        CancellationToken cancellationToken = default);
}
