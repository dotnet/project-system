// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Implementations may influence how dependencies are added and removed from snapshots.
    /// </summary>
    /// <remarks>
    /// When snapshot being updated with new, changed, removed dependencies it calls
    /// a list of available filters which can changed new/old items depending on particular
    /// filter condition.
    /// Filter can also prevent snapshot from doing update for given dependency when needed.
    /// </remarks>
    internal interface IDependenciesSnapshotFilter
    {
        /// <summary>
        /// Called before adding a dependency to a snapshot.
        /// </summary>
        /// <param name="projectPath">Path to current project.</param>
        /// <param name="targetFramework">Target framework for which dependency was resolved.</param>
        /// <param name="dependency">The dependency to which filter should be applied.</param>
        /// <param name="worldBuilder">Builder for immutable world dictionary of updating snapshot.</param>
        /// <param name="topLevelBuilder">Top level dependencies list builder of updating snapshot.</param>
        /// <param name="subTreeProviders">All known subtree providers</param>
        /// <param name="projectItemSpecs">List of all items contained in project's xml at given moment, otherwise, <see langword="null"/> if we do not have any data.</param>
        /// <param name="filterAnyChanges"><see langword="true"/> if the returned dependency differs from <paramref name="dependency"/>, or if <paramref name="worldBuilder"/> or <paramref name="topLevelBuilder"/> changed, otherwise <see langword="false"/>.</param>
        /// <returns>
        /// The dependency to be added, or <see langword="null"/> if the filter prohibits adding the dependency.
        /// Implementations may return a modified dependency to be added to the snapshot.
        /// </returns>
        IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs,
            out bool filterAnyChanges);

        /// <summary>
        /// Called before removing a dependency from a snapshot.
        /// </summary>
        /// <param name="projectPath">Path to current project.</param>
        /// <param name="targetFramework">Target framework for which dependency was resolved.</param>
        /// <param name="dependency">The dependency to which filter should be applied.</param>
        /// <param name="worldBuilder">Builder for immutable world dictionary of updating snapshot.</param>
        /// <param name="topLevelBuilder">Top level dependencies list builder of updating snapshot.</param>
        /// <param name="filterAnyChanges"><see langword="true"/> if <paramref name="worldBuilder"/> or <paramref name="topLevelBuilder"/> changed, otherwise <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if removal is approved, or <see langword="false"/> if <paramref name="dependency"/> should not be removed.</returns>
        bool BeforeRemove(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            out bool filterAnyChanges);
    }
}
