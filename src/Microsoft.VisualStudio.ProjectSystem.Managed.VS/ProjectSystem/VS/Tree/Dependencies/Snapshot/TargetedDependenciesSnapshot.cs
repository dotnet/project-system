// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal sealed class TargetedDependenciesSnapshot : ITargetedDependenciesSnapshot
    {
        #region Factories and internal constructor

        public static ITargetedDependenciesSnapshot CreateEmpty(string projectPath, ITargetFramework targetFramework, IProjectCatalogSnapshot catalogs)
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
        public static ITargetedDependenciesSnapshot FromChanges(
            string projectPath,
            ITargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string> projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            // catalogs can be null
            Requires.NotNull(changes, nameof(changes));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));
            // projectItemSpecs can be null

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
                        projectPath,
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
                IDependency dependency = new Dependency(dependencyModel, targetFramework, projectPath);

                context.Reset();

                foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                {
                    filter.BeforeAddOrUpdate(
                        projectPath,
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
            IProjectCatalogSnapshot catalogs,
            ImmutableDictionary<string, IDependency> dependenciesWorld)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            // catalogs can be null
            Requires.NotNull(dependenciesWorld, nameof(dependenciesWorld));

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;
            DependenciesWorld = dependenciesWorld;

            bool hasUnresolvedDependency = false;
            ImmutableArray<IDependency>.Builder topLevelDependencies = ImmutableArray.CreateBuilder<IDependency>();

            foreach ((string id, IDependency dependency) in dependenciesWorld)
            {
                System.Diagnostics.Debug.Assert(
                    string.Equals(id, dependency.Id),
                    "dependenciesWorld dictionary entry keys must match their value's ids.");

                if (!dependency.Resolved)
                {
                    hasUnresolvedDependency = true;
                }

                if (dependency.TopLevel)
                {
                    topLevelDependencies.Add(dependency);

                    if (!string.IsNullOrEmpty(dependency.Path))
                    {
                        _topLevelDependenciesByPathMap.Add(
                            Dependency.GetID(TargetFramework, dependency.ProviderType, dependency.Path),
                            dependency);
                    }
                }
            }

            HasUnresolvedDependency = hasUnresolvedDependency;
            TopLevelDependencies = topLevelDependencies.ToImmutable();
        }

        #endregion

        /// <inheritdoc />
        public string ProjectPath { get; }

        /// <inheritdoc />
        public ITargetFramework TargetFramework { get; }

        /// <inheritdoc />
        public IProjectCatalogSnapshot Catalogs { get; }

        /// <inheritdoc />
        public ImmutableArray<IDependency> TopLevelDependencies { get; }

        /// <inheritdoc />
        public ImmutableDictionary<string, IDependency> DependenciesWorld { get; }

        private readonly Dictionary<string, IDependency> _topLevelDependenciesByPathMap = new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ImmutableArray<IDependency>> _dependenciesChildrenMap = new Dictionary<string, ImmutableArray<IDependency>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _unresolvedDescendantsMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Re-use an existing, private, object reference for locking, rather than allocating a dedicated object.</summary>
        private object SyncLock => _dependenciesChildrenMap;

        /// <inheritdoc />
        public bool HasUnresolvedDependency { get; }

        /// <inheritdoc />
        public bool CheckForUnresolvedDependencies(IDependency dependency)
        {
            lock (SyncLock)
            {
                if (!_unresolvedDescendantsMap.TryGetValue(dependency.Id, out bool unresolved))
                {
                    unresolved = _unresolvedDescendantsMap[dependency.Id] = FindUnresolvedDependenciesRecursive(dependency);
                }

                return unresolved;
            }

            bool FindUnresolvedDependenciesRecursive(IDependency parent)
            {
                if (parent.DependencyIDs.Count == 0)
                {
                    return false;
                }

                foreach (IDependency child in GetDependencyChildren(parent))
                {
                    if (!child.Resolved)
                    {
                        return true;
                    }

                    // If the dependency is already in the child map, it is resolved
                    // Checking here will prevent a stack overflow due to rechecking the same dependencies
                    if (_dependenciesChildrenMap.ContainsKey(child.Id))
                    {
                        return false;
                    }

                    if (!_unresolvedDescendantsMap.TryGetValue(child.Id, out bool depthFirstResult))
                    {
                        depthFirstResult = FindUnresolvedDependenciesRecursive(child);
                        _unresolvedDescendantsMap[parent.Id] = depthFirstResult;
                        return depthFirstResult;
                    }

                    if (depthFirstResult)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool CheckForUnresolvedDependencies(string providerType)
        {
            foreach ((string _, IDependency dependency) in DependenciesWorld)
            {
                if (StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, providerType) &&
                    !dependency.Resolved)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public ImmutableArray<IDependency> GetDependencyChildren(IDependency dependency)
        {
            if (dependency.DependencyIDs.Count == 0)
            {
                return ImmutableArray<IDependency>.Empty;
            }

            lock (SyncLock)
            {
                if (!_dependenciesChildrenMap.TryGetValue(dependency.Id, out ImmutableArray<IDependency> children))
                {
                    children = _dependenciesChildrenMap[dependency.Id] = BuildChildren();
                }

                return children;
            }

            ImmutableArray<IDependency> BuildChildren()
            {
                ImmutableArray<IDependency>.Builder children =
                    ImmutableArray.CreateBuilder<IDependency>(dependency.DependencyIDs.Count);

                foreach (string id in dependency.DependencyIDs)
                {
                    if (DependenciesWorld.TryGetValue(id, out IDependency child) ||
                        _topLevelDependenciesByPathMap.TryGetValue(id, out child))
                    {
                        children.Add(child);
                    }
                }

                return children.Count == children.Capacity
                    ? children.MoveToImmutable()
                    : children.ToImmutable();
            }
        }
    }
}
