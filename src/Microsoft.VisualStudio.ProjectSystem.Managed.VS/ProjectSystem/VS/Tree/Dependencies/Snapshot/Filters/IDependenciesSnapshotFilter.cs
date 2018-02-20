// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// When snapshot being updated with new, changed, removed dependencies it calls
    /// a list of available filters which can changed new/old items depending on particular
    /// filter condition.
    /// Filter can also prevent snapshot from doing update for given dependency when needed.
    /// </summary>
    internal interface IDependenciesSnapshotFilter
    {
        /// <summary>
        /// Is called before adding a given dependency.
        /// </summary>
        /// <returns>
        ///     Returns original or modified dependency depending on filter's logic. 
        ///     If returns null, snapshot does not do any updates for this dependency.
        /// </returns>
        /// <param name="projectPath">Path to current project.</param>
        /// <param name="targetFramework">Target framework for which dependency was resolved.</param>
        /// <param name="dependency">The dependency to which filter should be applied.</param>
        /// <param name="worldBuilder">Builder for immutable world dictionary of updating snapshot.</param>
        /// <param name="topLevelBuilder">Top level dependencies list builder of updating snapshot.</param>
        /// <param name="subTreeProviders">All known subtree providers</param>
        /// <param name="projectItemSpecs">List of all items contained in project's xml at given moment, otherwise, <see langword="null"/> if we do not have any data.</param>
        /// <param name="filterAnyChanges">True if filter did any changes in the snapshot</param>
        /// <returns>Dependency to be added if addition is approved, null if dependency should not be added to snapshot</returns>
        IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency, 
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            Dictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs,
            out bool filterAnyChanges);

        /// <summary>
        /// Is called before removing a given dependecy.
        /// </summary>
        /// <param name="projectPath">Path to current project.</param>
        /// <param name="targetFramework">Target framework for which dependency was resolved.</param>
        /// <param name="dependency">The dependency to which filter should be applied.</param>
        /// <param name="worldBuilder">Builder for immutable world dictionary of updating snapshot.</param>
        /// <param name="topLevelBuilder">Top level dependencies list builder of updating snapshot.</param>
        /// <param name="filterAnyChanges">True if filter did any changes in the snapshot</param>
        /// <returns>Dependency itself if removal is approved, null if dependency should not be removed</returns>
        IDependency BeforeRemove(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency, 
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            out bool filterAnyChanges);
    }
}
