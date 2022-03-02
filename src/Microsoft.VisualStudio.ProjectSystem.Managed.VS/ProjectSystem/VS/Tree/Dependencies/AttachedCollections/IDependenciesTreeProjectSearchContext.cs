// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides services throughout the lifetime of a search operation.
    /// </summary>
    public interface IDependenciesTreeProjectSearchContext
    {
        /// <summary>
        /// Gets a token that signals cancellation of the ongoing search operation.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the unconfigured project being searched.
        /// </summary>
        UnconfiguredProject UnconfiguredProject { get; }

        /// <summary>
        /// Gets a sub-context specific to a given project configuration.
        /// </summary>
        /// <param name="configuredProject">The configured project being searched.</param>
        /// <param name="cancellationToken">Allows cancellation of the operation.</param>
        /// <returns>The sub-context for the configuration, or <see langword="null"/> if not found.</returns>
        Task<IDependenciesTreeConfiguredProjectSearchContext?> ForConfiguredProjectAsync(
            ConfiguredProject configuredProject,
            CancellationToken cancellationToken = default);
    }
}
