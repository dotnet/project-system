// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal sealed class TargetedDependenciesSnapshot
    {
        #region Factories and internal constructor

        public static TargetedDependenciesSnapshot CreateEmpty(string projectPath, ITargetFramework targetFramework, IProjectCatalogSnapshot? catalogs)
        {
            return new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableStringDictionary<IDependency>.EmptyOrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies changes to <paramref name="previousSnapshot"/> and produces a new snapshot if required.
        /// If no changes are made, <paramref name="previousSnapshot"/> is returned unmodified.
        /// </summary>
        /// <returns>An updated snapshot, or <paramref name="previousSnapshot"/> if no changes occured.</returns>
        public static TargetedDependenciesSnapshot FromChanges(
            string projectPath,
            TargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changes, nameof(changes));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            bool anyChanges = false;

            ITargetFramework targetFramework = previousSnapshot.TargetFramework;

            var worldBuilder = previousSnapshot.DependenciesWorld.ToBuilder();

            if (changes.RemovedNodes.Count != 0)
            {
                var context = new RemoveDependencyContext(worldBuilder);

                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    Remove(context, removed);
                }
            }

            if (changes.AddedNodes.Count != 0)
            {
                var context = new AddDependencyContext(worldBuilder);

                foreach (IDependencyModel added in changes.AddedNodes)
                {
                    Add(context, added);
                }
            }

            // Also factor in any changes to path/framework/catalogs
            anyChanges =
                anyChanges ||
                !StringComparers.Paths.Equals(projectPath, previousSnapshot.ProjectPath) ||
                !targetFramework.Equals(previousSnapshot.TargetFramework) ||
                !Equals(catalogs, previousSnapshot.Catalogs);

            if (anyChanges)
            {
                return new TargetedDependenciesSnapshot(
                    projectPath,
                    targetFramework,
                    catalogs,
                    worldBuilder.ToImmutable());
            }

            return previousSnapshot;

            void Remove(RemoveDependencyContext context, IDependencyModel dependencyModel)
            {
                string dependencyId = Dependency.GetID(
                    targetFramework, dependencyModel.ProviderType, dependencyModel.Id);

                if (!context.TryGetDependency(dependencyId, out IDependency dependency))
                {
                    return;
                }

                context.Reset();

                foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                {
                    filter.BeforeRemove(
                        targetFramework,
                        dependency,
                        context);

                    anyChanges |= context.Changed;

                    if (!context.GetResult(filter))
                    {
                        // TODO breaking here denies later filters the opportunity to modify builders
                        return;
                    }
                }

                worldBuilder.Remove(dependencyId);
                anyChanges = true;
            }

            void Add(AddDependencyContext context, IDependencyModel dependencyModel)
            {
                // Create the unfiltered dependency
                IDependency? dependency = new Dependency(dependencyModel, targetFramework, projectPath);

                context.Reset();

                foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                {
                    filter.BeforeAddOrUpdate(
                        targetFramework,
                        dependency,
                        subTreeProviderByProviderType,
                        projectItemSpecs,
                        context);

                    dependency = context.GetResult(filter);

                    if (dependency == null)
                    {
                        break;
                    }
                }

                if (dependency != null)
                {
                    // A dependency was accepted
                    worldBuilder.Remove(dependency.Id);
                    worldBuilder.Add(dependency.Id, dependency);
                    anyChanges = true;
                }
                else
                {
                    // Even though the dependency was rejected, it's possible that filters made
                    // changes to other dependencies.
                    anyChanges |= context.Changed;
                }
            }
        }

        // Internal, for test use -- normal code should use the factory methods
        internal TargetedDependenciesSnapshot(
            string projectPath,
            ITargetFramework targetFramework,
            IProjectCatalogSnapshot? catalogs,
            ImmutableDictionary<string, IDependency> dependenciesWorld)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNull(dependenciesWorld, nameof(dependenciesWorld));
            Assumes.True(Equals(dependenciesWorld.KeyComparer, StringComparers.DependencyTreeIds), $"{nameof(dependenciesWorld)} must have an {nameof(StringComparers.DependencyTreeIds)} key comparer.");

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;
            DependenciesWorld = dependenciesWorld;

            bool hasVisibleUnresolvedDependency;

            // Perform a single pass through dependencies, gathering as much information as possible
            (TopLevelDependencies, _topLevelDependencyByPath, hasVisibleUnresolvedDependency) = Scan(dependenciesWorld, targetFramework);

            if (hasVisibleUnresolvedDependency)
            {
                // Walk the dependency graph to find visible, unresolved dependencies which are reachable
                // from visible top-level dependencies.
                _hasReachableVisibleUnresolvedById = FindReachableVisibleUnresolvedDependencies();
            }
            else
            {
                // There are no visible and unresolved dependencies in the snapshot
                _hasReachableVisibleUnresolvedById = null;
            }

            return;

            static (ImmutableArray<IDependency> TopLevelDependencies, Dictionary<string, IDependency> topLevelDependencyByPath, bool hasVisibleUnresolvedDependency) Scan(ImmutableDictionary<string, IDependency> dependenciesWorld, ITargetFramework targetFramework)
            {
                // TODO use ToImmutableAndFree?
                ImmutableArray<IDependency>.Builder topLevelDependencies = ImmutableArray.CreateBuilder<IDependency>();

                bool hasVisibleUnresolvedDependency = false;
                var topLevelDependencyByPath = new Dictionary<string, IDependency>(StringComparers.DependencyTreeIds);

                foreach ((string id, IDependency dependency) in dependenciesWorld)
                {
                    System.Diagnostics.Debug.Assert(
                        string.Equals(id, dependency.Id),
                        "dependenciesWorld dictionary entry keys must match their value's ids.");

                    if (!dependency.Resolved && dependency.Visible)
                    {
                        hasVisibleUnresolvedDependency = true;
                    }

                    if (dependency.TopLevel)
                    {
                        topLevelDependencies.Add(dependency);

                        if (!string.IsNullOrEmpty(dependency.Path))
                        {
                            topLevelDependencyByPath.Add(
                                Dependency.GetID(targetFramework, dependency.ProviderType, dependency.Path),
                                dependency);
                        }
                    }
                }

                return (topLevelDependencies.ToImmutable(), topLevelDependencyByPath, hasVisibleUnresolvedDependency);
            }

            IReadOnlyDictionary<string, bool>? FindReachableVisibleUnresolvedDependencies()
            {
                // It is possible that there exists a dependency which is both visible and unresolved, yet not
                // actually present in the tree because one of its ancestors is not visible. Therefore instead
                // of scanning all dependencies in the snapshot, we walk the dependency graph starting with
                // top-level visible nodes, and remember those which have at least one visible, unresolved and
                // reachable descendant.

                bool hasReachableVisibleUnresolvedDependency = false;

                var hasReachableVisibleUnresolvedById = new Dictionary<string, bool>(StringComparers.DependencyTreeIds);

                // 'spine' is a stack containing an enumerator for each level of the graph, which is updated as
                // we walk the graph. We are working with struct enumerators so need this array. It will grow
                // if needed.
                var spine = new ImmutableArray<IDependency>.Enumerator[8];
                int depth = 0;

                // Start with all top-level dependencies
                spine[0] = TopLevelDependencies.GetEnumerator();

                while (true)
                {
                    ref ImmutableArray<IDependency>.Enumerator level = ref spine[depth];

                    // Move to next item at this level
                    if (!level.MoveNext())
                    {
                        // This level is done, so pop back up a level
                        depth--;

                        if (depth < 0)
                        {
                            // No more levels, so finished tree traversal
                            break;
                        }

                        // Resume the previous level
                        continue;
                    }

                    // Wvaluate the current dependency
                    IDependency dependency = level.Current;

                    if (!dependency.Visible)
                    {
                        // Skip any hidden nodes
                        continue;
                    }

                    if (hasReachableVisibleUnresolvedById.ContainsKey(dependency.Id))
                    {
                        // We've already visited this item, so skip it
                        continue;
                    }

                    if (!dependency.Resolved)
                    {
                        // This node is unresolved
                        hasReachableVisibleUnresolvedDependency = true;

                        // This node is unresolved, so set it and all items up the spine as unresolved
                        for (int i = 0; i <= depth; i++)
                        {
                            hasReachableVisibleUnresolvedById[spine[i].Current.Id] = true;
                        }
                    }
                    else
                    {
                        // This node is resolved, set it to false. If a descendant is later
                        // found which is visible, reachable and unresolved, then this dependency's
                        // entry will be updated to 'true' as part of marking all entries in the spine.
                        hasReachableVisibleUnresolvedById[dependency.Id] = false;
                    }

                    if (dependency.DependencyIDs.Length != 0)
                    {
                        // This dependency has child dependencies, so traverse into them
                        depth++;

                        if (depth == spine.Length)
                        {
                            // Grow the spine
                            var newSpine = new ImmutableArray<IDependency>.Enumerator[depth << 1];
                            Array.Copy(spine, newSpine, depth);
                            spine = newSpine;
                        }

                        // Enumerate child dependencies
                        spine[depth] = GetDependencyChildren(dependency).GetEnumerator();
                    }
                }

                // If, after all that, all visible and reachable dependencies are resolved, return
                // null as there is no value in a collection where every entry is false. Consumers
                // of this collection are local to this class and will null appropriately.
                return hasReachableVisibleUnresolvedDependency
                    ? hasReachableVisibleUnresolvedById
                    : null;
            }
        }

        #endregion

        /// <summary>
        /// Path to project containing this snapshot.
        /// </summary>
        public string ProjectPath { get; }

        /// <summary>
        /// <see cref="ITargetFramework" /> for which project has dependencies contained in this snapshot.
        /// </summary>
        public ITargetFramework TargetFramework { get; }

        /// <summary>
        /// Catalogs of rules for project items (optional, custom dependency providers might not provide it).
        /// </summary>
        public IProjectCatalogSnapshot? Catalogs { get; }

        /// <summary>
        /// Top level project dependencies.
        /// </summary>
        public ImmutableArray<IDependency> TopLevelDependencies { get; }

        /// <summary>
        /// Contains all unique <see cref="IDependency"/> objects in the project, from all levels.
        /// Allows looking them up by their IDs.
        /// </summary>
        public ImmutableDictionary<string, IDependency> DependenciesWorld { get; }

        /// <summary>
        /// Maps each top-level dependency by its path, where path is composed of targetFramework/providerType/dependencyPath.
        /// </summary>
        private readonly Dictionary<string, IDependency> _topLevelDependencyByPath;

        /// <summary>
        /// A map whose keys are the IDs of all reachable dependencies, and whose values are <see langword="true" /> if
        /// the dependency has a reachable, visible, unresolved descendant, otherwise <see langword="false" />.
        /// If this fields is <see langword="null" /> then there are no reachable visible unresolved dependencies in the
        /// entire snapshot.
        /// Any dependency not in this collection will not be displayed in the tree.
        /// </summary>
        private readonly IReadOnlyDictionary<string, bool>? _hasReachableVisibleUnresolvedById;

        /// <summary>
        /// Gets whether this snapshot contains at least one unresolved dependency which is both visible
        /// and reachable from a visible top-level dependency.
        /// </summary>
        public bool HasReachableVisibleUnresolvedDependency => _hasReachableVisibleUnresolvedById != null;

        /// <summary>
        /// Gets whether this dependency's node should appear as unresolved in the dependencies tree.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="true" /> if, for either <paramref name="dependency"/> or one of its descendants, all of the following are true:
        /// <list type="number">
        ///   <item><see cref="IDependency.Visible"/> is <see langword="true" />, and</item>
        ///   <item><see cref="IDependency.Resolved"/> is <see langword="false" />, and</item>
        ///   <item>the dependency is reachable via the dependency graph from a visible top-level node, where all intermediate nodes are also visible.</item>
        /// </list>
        /// </remarks>
        public bool ShouldAppearUnresolved(IDependency dependency)
        {
            if (_hasReachableVisibleUnresolvedById == null)
            {
                // No reachable dependency in this snapshot is visible and unresolved
                return false;
            }

            if (_hasReachableVisibleUnresolvedById.TryGetValue(dependency.Id, out bool exists))
            {
                return exists;
            }

            System.Diagnostics.Debug.Fail("Snapshot should not be asked about unreachable dependency, or dependency not in snapshot.");
            return !dependency.Resolved;
        }

        /// <summary>
        /// Efficient API for checking if a there is at least one unresolved dependency with given provider type.
        /// </summary>
        /// <param name="providerType">Provider type to check</param>
        /// <returns>Returns true if there is at least one unresolved dependency with given providerType.</returns>
        public bool CheckForUnresolvedDependencies(string providerType)
        {
            if (_hasReachableVisibleUnresolvedById == null)
            {
                return false;
            }

            foreach ((string _, IDependency dependency) in DependenciesWorld)
            {
                if (StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, providerType) &&
                    dependency.Visible &&
                    !dependency.Resolved &&
                    _hasReachableVisibleUnresolvedById.ContainsKey(dependency.Id))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of direct child nodes for given dependency
        /// </summary>
        /// <param name="dependency"></param>
        public ImmutableArray<IDependency> GetDependencyChildren(IDependency dependency)
        {
            if (dependency.DependencyIDs.Length == 0)
            {
                return ImmutableArray<IDependency>.Empty;
            }

            ImmutableArray<IDependency>.Builder children = ImmutableArray.CreateBuilder<IDependency>(dependency.DependencyIDs.Length);

            foreach (string id in dependency.DependencyIDs)
            {
                // TODO what if a dependency's child isn't in the snapshot? is that a bug?
                // TODO why is the ID also considered a path there?
                if (DependenciesWorld.TryGetValue(id, out IDependency child) || _topLevelDependencyByPath.TryGetValue(id, out child))
                {
                    children.Add(child);
                }
            }

            return children.Count == children.Capacity
                ? children.MoveToImmutable()
                : children.ToImmutable();
        }

        public override string ToString() => $"{TargetFramework.FriendlyName} - {DependenciesWorld.Count} dependencies ({TopLevelDependencies.Length} top level) - {ProjectPath}";
    }
}
