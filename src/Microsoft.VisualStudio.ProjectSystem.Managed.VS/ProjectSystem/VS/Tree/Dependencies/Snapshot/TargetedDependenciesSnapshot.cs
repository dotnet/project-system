// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal class TargetedDependenciesSnapshot : ITargetedDependenciesSnapshot
    {
        private static readonly IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> s_emptySubTreeProviderMap
            = ImmutableDictionary.Create<string, IProjectDependenciesSubTreeProvider>(StringComparers.DependencyProviderTypes);

        protected TargetedDependenciesSnapshot(
            string projectPath,
            ITargetFramework targetFramework,
            ITargetedDependenciesSnapshot previousSnapshot = null,
            IProjectCatalogSnapshot catalogs = null)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;

            if (previousSnapshot != null)
            {
                TopLevelDependencies = previousSnapshot.TopLevelDependencies;
                DependenciesWorld = previousSnapshot.DependenciesWorld;
            }
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal TargetedDependenciesSnapshot(IDictionary<string, IDependency> dependenciesWorld, IEnumerable<IDependency> topLevelDependencies)
        {
            DependenciesWorld = dependenciesWorld.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
            TopLevelDependencies = topLevelDependencies.ToImmutableHashSet();
        }

        public string ProjectPath { get; }

        public ITargetFramework TargetFramework { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public ImmutableHashSet<IDependency> TopLevelDependencies { get; private set; } = ImmutableHashSet<IDependency>.Empty;

        public ImmutableDictionary<string, IDependency> DependenciesWorld { get; private set; } = ImmutableStringDictionary<IDependency>.EmptyOrdinalIgnoreCase;

        private readonly Dictionary<string, IDependency> _topLevelDependenciesByPathMap = new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ImmutableArray<IDependency>> _dependenciesChildrenMap = new ConcurrentDictionary<string, ImmutableArray<IDependency>>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _unresolvedDescendantsMap = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private bool? _hasUnresolvedDependency;

        public bool HasUnresolvedDependency
        {
            get
            {
                if (_hasUnresolvedDependency == null)
                {
                    _hasUnresolvedDependency = 
                        TopLevelDependencies.Any(x => !x.Resolved) || 
                        DependenciesWorld.Values.Any(x => !x.Resolved);
                }

                return _hasUnresolvedDependency.Value;
            }
        }

        public bool CheckForUnresolvedDependencies(IDependency dependency)
        {
            if (_unresolvedDescendantsMap.TryGetValue(dependency.Id, out bool hasUnresolvedDescendants))
            {
                return hasUnresolvedDescendants;
            }

            return FindUnresolvedDependenciesRecursive(dependency);
        }

        public bool CheckForUnresolvedDependencies(string providerType)
        {
            return DependenciesWorld.Values.Any(
                x => StringComparers.DependencyProviderTypes.Equals(x.ProviderType, providerType) && !x.Resolved);
        }

        public IEnumerable<IDependency> GetDependencyChildren(IDependency dependency)
        {
            return _dependenciesChildrenMap.GetOrAdd(dependency.Id, _ =>
            {
                ImmutableArray<IDependency>.Builder children =
                    ImmutableArray.CreateBuilder<IDependency>(dependency.DependencyIDs.Count);

                foreach (string id in dependency.DependencyIDs)
                {
                    if (TryToFindDependency(id, out IDependency child))
                    {
                        children.Add(child);
                    }
                }

                return children.Count == children.Capacity
                    ? children.MoveToImmutable()
                    : children.ToImmutable();
            });

            bool TryToFindDependency(string id, out IDependency dep)
            {
                return DependenciesWorld.TryGetValue(id, out dep) ||
                       _topLevelDependenciesByPathMap.TryGetValue(id, out dep);
            }
        }

        private bool FindUnresolvedDependenciesRecursive(IDependency dependency)
        {
            bool unresolved = false;

            if (dependency.DependencyIDs.Count > 0)
            {
                foreach (IDependency child in GetDependencyChildren(dependency))
                {
                    if (!child.Resolved)
                    {
                        unresolved = true;
                        break;
                    }

                    // If the dependency is already in the child map, it is resolved
                    // Checking here will prevent a stack overflow due to rechecking the same dependencies
                    if (_dependenciesChildrenMap.ContainsKey(child.Id))
                    {
                        unresolved = false;
                        break;
                    }

                    if (!_unresolvedDescendantsMap.TryGetValue(child.Id, out bool depthFirstResult))
                    {
                        depthFirstResult = FindUnresolvedDependenciesRecursive(child);
                    }

                    if (depthFirstResult)
                    {
                        unresolved = true;
                        break;
                    }
                }
            }

            _unresolvedDescendantsMap[dependency.Id] = unresolved;
            return unresolved;
        }

        private bool MergeChanges(
            IDependenciesChanges changes,
            IReadOnlyCollection<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyCollection<IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs)
        {
            var worldBuilder = DependenciesWorld.ToBuilder();
            var topLevelBuilder = TopLevelDependencies.ToBuilder();

            bool anyChanges = false;

            foreach (IDependencyModel removed in changes.RemovedNodes)
            {
                string targetedId = Dependency.GetID(TargetFramework, removed.ProviderType, removed.Id);

                if (!worldBuilder.TryGetValue(targetedId, out IDependency dependency))
                {
                    continue;
                }

                bool canRemove = true;

                if (snapshotFilters != null)
                {
                    foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                    {
                        canRemove = filter.BeforeRemove(
                            ProjectPath, TargetFramework, dependency, worldBuilder, topLevelBuilder, out bool filterAnyChanges);

                        anyChanges |= filterAnyChanges;

                        if (!canRemove)
                        {
                            // TODO breaking here denies later filters the opportunity to modify builders
                            break;
                        }
                    }
                }

                if (canRemove)
                {
                    anyChanges = true;
                    worldBuilder.Remove(targetedId);
                    topLevelBuilder.Remove(dependency);
                }
            }

            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProvidersMap
                = subTreeProviders?.ToDictionary(p => p.ProviderType, StringComparers.DependencyProviderTypes)
                  ?? s_emptySubTreeProviderMap;

            foreach (IDependencyModel added in changes.AddedNodes)
            {
                IDependency newDependency = new Dependency(added, TargetFramework, ProjectPath);

                if (snapshotFilters != null)
                {
                    foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                    {
                        newDependency = filter.BeforeAdd(
                            ProjectPath,
                            TargetFramework,
                            newDependency,
                            worldBuilder,
                            topLevelBuilder,
                            subTreeProvidersMap,
                            projectItemSpecs,
                            out bool filterAnyChanges);

                        anyChanges |= filterAnyChanges;

                        if (newDependency == null)
                        {
                            break;
                        }
                    }
                }

                if (newDependency == null)
                {
                    continue;
                }

                anyChanges = true;

                worldBuilder[newDependency.Id] = newDependency;

                if (newDependency.TopLevel)
                {
                    topLevelBuilder.Add(newDependency);
                }
            }

            DependenciesWorld = worldBuilder.ToImmutable();
            TopLevelDependencies = topLevelBuilder.ToImmutable();

            ConstructTopLevelDependenciesByPathMap();

            return anyChanges;

            void ConstructTopLevelDependenciesByPathMap()
            {
                foreach (IDependency topLevelDependency in TopLevelDependencies)
                {
                    if (!string.IsNullOrEmpty(topLevelDependency.Path))
                    {
                        _topLevelDependenciesByPathMap.Add(
                            Dependency.GetID(TargetFramework, topLevelDependency.ProviderType, topLevelDependency.Path),
                            topLevelDependency);
                    }
                }
            }
        }

        public static TargetedDependenciesSnapshot FromChanges(
            string projectPath,
            ITargetFramework targetFramework,
            ITargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            IReadOnlyCollection<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyCollection<IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs,
            out bool anyChanges)
        {
            var newSnapshot = new TargetedDependenciesSnapshot(projectPath, targetFramework, previousSnapshot, catalogs);
            anyChanges = newSnapshot.MergeChanges(changes, snapshotFilters, subTreeProviders, projectItemSpecs);
            return newSnapshot;
        }
    }
}
