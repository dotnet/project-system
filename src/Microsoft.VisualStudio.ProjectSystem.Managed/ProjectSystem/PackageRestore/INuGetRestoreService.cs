// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
///     Responsible for handling NuGet package restore requests.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface INuGetRestoreService
{
    /// <summary>
    ///     Requests that the <see cref="INuGetRestoreService"/> restore the project using the data in the <paramref name="restoreData"/>.
    /// </summary>
    /// <param name="restoreData">
    ///     The data needed to run the restore information itself.
    /// </param>
    /// <param name="inputVersions">
    ///     The <see cref="ConfiguredProject"/> version information corresponding to <paramref name="restoreData"/>.
    ///     Implementations can use this to determine if there is pending work that could lead to a restore in
    ///     the near future.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token indicating when the restore operation should be cancelled (e.g. due to the project being
    ///     unloaded).
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}"/> indicating when the work associated with the restore request, if any,
    ///     has completed. The <see cref="Task{TResult}.Result"/> will be <see langword="true"/> if the restore
    ///     ran and completed successfully, and <see langword="false"/> otherwise.
    /// </returns>
    Task<bool> NominateAsync(ProjectRestoreInfo restoreData, IReadOnlyCollection<PackageRestoreConfiguredInput> inputVersions, CancellationToken cancellationToken);

    /// <summary>
    ///     Informs the <see cref="INuGetRestoreService"/> that a newer version of <see cref="ProjectRestoreInfo"/>
    ///     is available, but the data is identical to what was previously provided to <see cref="NominateAsync(ProjectRestoreInfo, IReadOnlyCollection{PackageRestoreConfiguredInput}, CancellationToken)"/>
    ///     and no restore is needed.
    /// </summary>
    /// <remarks>
    ///     This method exists to help the <see cref="INuGetRestoreService"/> track when work is being done that
    ///     might lead to a nomination. If the version information in the <paramref name="inputVersions"/>
    ///     corresponds to the latest version of each <see cref="ConfiguredProject"/> then there is no pending
    ///     work that may lead to a nomination. On the other hand, if the version information is older then a
    ///     nomination may occur in the near future.
    /// </remarks>
    Task UpdateWithoutNominationAsync(IReadOnlyCollection<PackageRestoreConfiguredInput> inputVersions);

    /// <summary>
    ///     Indicates that there will be no further calls to <see cref="NominateAsync(ProjectRestoreInfo, IReadOnlyCollection{PackageRestoreConfiguredInput}, CancellationToken)"/>
    ///     or <see cref="UpdateWithoutNominationAsync(IReadOnlyCollection{PackageRestoreConfiguredInput})"/>.
    /// </summary>
    /// <remarks>
    ///     Called as a hint that any work waiting for a possible future nomination can be cancelled.
    /// </remarks>
    void NotifyComplete();

    /// <summary>
    ///     Indicates that there will be no further calls to <see cref="NominateAsync(ProjectRestoreInfo, IReadOnlyCollection{PackageRestoreConfiguredInput}, CancellationToken)"/>
    ///     or <see cref="UpdateWithoutNominationAsync(IReadOnlyCollection{PackageRestoreConfiguredInput})"/> due to some underlying fault.
    /// </summary>
    /// <remarks>
    ///     Called as a hint that any work waiting for a possible future nomination should be failed due to the
    ///     exception <paramref name="e"/>.
    /// </remarks>
    void NotifyFaulted(Exception e);
}
