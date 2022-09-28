// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Persists fast up-to-date check state across solution lifetimes.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.OneOrZero)]
    internal interface IUpToDateCheckStatePersistence
    {
        /// <summary>
        /// Retrieves the stored up-to-date check state for a given configured project related to the set of project items.
        /// </summary>
        /// <param name="projectPath">The full path of the project.</param>
        /// <param name="configurationDimensions">The map of dimension names and values that describes the project configuration.</param>
        /// <param name="cancellationToken">Allows cancelling this asynchronous operation.</param>
        /// <returns>The hash and time at which items were last known to have changed (in UTC), or <see langword="null"/> if unknown.</returns>
        Task<(int ItemHash, DateTime? ItemsChangedAtUtc)?> RestoreItemStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the stored up-to-date check state for a given configured project.
        /// </summary>
        /// <remarks>
        /// This value is required in order to protect against the race condition described in
        /// https://github.com/dotnet/project-system/issues/4014. If source files are modified
        /// during a compilation, but before that compilation's outputs are produced, then the
        /// changed input file's timestamp will be earlier than the compilation output, making
        /// it seem as though the compilation is up to date when in fact the input was not
        /// included in that compilation. Comparing against compilation start time fixes this
        /// issue.
        /// </remarks>
        /// <param name="projectPath">The full path of the project.</param>
        /// <param name="configurationDimensions">The map of dimension names and values that describes the project configuration.</param>
        /// <param name="cancellationToken">Allows cancelling this asynchronous operation.</param>
        /// <returns>The time as which the last successful build started (in UTC), or <see langword="null"/> if unknown.</returns>
        Task<DateTime?> RestoreLastSuccessfulBuildStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, CancellationToken cancellationToken);

        /// <summary>
        /// Stores up-to-date check state for a given configured project, related to the set of project items.
        /// </summary>
        /// <param name="projectPath">The full path of the project.</param>
        /// <param name="configurationDimensions">The map of dimension names and values that describes the project configuration.</param>
        /// <param name="itemHash">The hash of items to be stored.</param>
        /// <param name="itemsChangedAtUtc">The time at which items were last known to have changed (in UTC), or <see langword="null"/> if we haven't observed them change.</param>
        /// <param name="cancellationToken">Allows cancelling this asynchronous operation.</param>
        /// <returns>A task that completes when this operation has finished.</returns>
        Task StoreItemStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, int itemHash, DateTime? itemsChangedAtUtc, CancellationToken cancellationToken);

        /// <summary>
        /// Stores up-to-date check state for a given configured project, related to the last successful build time.
        /// </summary>
        /// <param name="projectPath">The full path of the project.</param>
        /// <param name="configurationDimensions">The map of dimension names and values that describes the project configuration.</param>
        /// <param name="lastSuccessfulBuildStartedAtUtc">The time at which the project's last successful build started (in UTC).</param>
        /// <param name="cancellationToken">Allows cancelling this asynchronous operation.</param>
        /// <returns>A task that completes when this operation has finished.</returns>
        Task StoreLastSuccessfulBuildStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, DateTime lastSuccessfulBuildStartedAtUtc, CancellationToken cancellationToken);
    }
}
