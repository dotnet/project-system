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
    internal sealed class TargetedDependenciesSnapshot : ITargetedDependenciesSnapshot
    {
        private static readonly IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> s_emptySubTreeProviderMap
            = ImmutableDictionary.Create<string, IProjectDependenciesSubTreeProvider>(StringComparers.DependencyProviderTypes);

        #region Factories and internal constructor

        public static ITargetedDependenciesSnapshot CreateEmpty(string projectPath, ITargetFramework targetFramework, IProjectCatalogSnapshot catalogs)
        {
            return new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableHashSet<IDependency>.Empty,
                ImmutableStringDictionary<IDependency>.EmptyOrdinalIgnoreCase);
        }

        public static ITargetedDependenciesSnapshot FromChanges(
            string projectPath,
            ITargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            IReadOnlyCollection<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyCollection<IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs,
            out bool anyChanges)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changes, nameof(changes));

            anyChanges = false;

            ITargetFramework targetFramework = previousSnapshot.TargetFramework;

            var worldBuilder = previousSnapshot.DependenciesWorld.ToBuilder();
            var topLevelBuilder = previousSnapshot.TopLevelDependencies.ToBuilder();

            foreach (IDependencyModel removed in changes.RemovedNodes)
            {
                string targetedId = Dependency.GetID(targetFramework, removed.ProviderType, removed.Id);

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
                            projectPath, targetFramework, dependency, worldBuilder, topLevelBuilder, out bool filterAnyChanges);

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
                IDependency newDependency = new Dependency(added, targetFramework, projectPath);

                if (snapshotFilters != null)
                {
                    foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                    {
                        newDependency = filter.BeforeAdd(
                            projectPath,
                            targetFramework,
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
                    topLevelBuilder.ToImmutable(),
                    worldBuilder.ToImmutable());
            }

            return previousSnapshot;
        }

        // Internal, for test use -- normal code should use the factory methods
        internal TargetedDependenciesSnapshot(
            string projectPath,
            ITargetFramework targetFramework,
            IProjectCatalogSnapshot catalogs,
            ImmutableHashSet<IDependency> topLevelDependencies,
            ImmutableDictionary<string, IDependency> dependenciesWorld)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            // catalogs can be null
            Requires.NotNull(topLevelDependencies, nameof(topLevelDependencies));
            Requires.NotNull(dependenciesWorld, nameof(dependenciesWorld));

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;
            TopLevelDependencies = topLevelDependencies;
            DependenciesWorld = dependenciesWorld;

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

        #endregion

        /// <inheritdoc />
        public string ProjectPath { get; }

        /// <inheritdoc />
        public ITargetFramework TargetFramework { get; }

        /// <inheritdoc />
        public IProjectCatalogSnapshot Catalogs { get; }

        /// <inheritdoc />
        public ImmutableHashSet<IDependency> TopLevelDependencies { get; }

        /// <inheritdoc />
        public ImmutableDictionary<string, IDependency> DependenciesWorld { get; }

        private readonly Dictionary<string, IDependency> _topLevelDependenciesByPathMap = new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ImmutableArray<IDependency>> _dependenciesChildrenMap = new ConcurrentDictionary<string, ImmutableArray<IDependency>>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _unresolvedDescendantsMap = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private bool? _hasUnresolvedDependency;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool CheckForUnresolvedDependencies(IDependency dependency)
        {
            if (_unresolvedDescendantsMap.TryGetValue(dependency.Id, out bool hasUnresolvedDescendants))
            {
                return hasUnresolvedDescendants;
            }

            return FindUnresolvedDependenciesRecursive(dependency);
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
        public IEnumerable<IDependency> GetDependencyChildren(IDependency dependency)
        {
            return GetDependencyChildrenInternal(dependency);
        }

        private ImmutableArray<IDependency> GetDependencyChildrenInternal(IDependency dependency)
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
                foreach (IDependency child in GetDependencyChildrenInternal(dependency))
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
    }
}
