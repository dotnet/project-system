// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

/// <summary>
/// Provides the .NET SDK version pinned by the solution's <c>global.json</c>, if any.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private)]
internal interface ISolutionSdkVersionProvider
{
    /// <summary>
    /// Returns the SDK version from the <c>global.json</c> that applies to the current solution, or
    /// <see langword="null"/> if there is no solution, no <c>global.json</c> above it, or the file does
    /// not specify <c>sdk.version</c>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    ///     The file system is walked once per solution. Callers see any later addition, change or deletion
    ///     of a <c>global.json</c> that could change the result.
    /// </remarks>
    Task<string?> GetSdkVersionAsync(CancellationToken cancellationToken = default);
}
