// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        protected TargetedDependenciesSnapshot(string projectPath,
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

        public string ProjectPath { get; }

        public ITargetFramework TargetFramework { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public ImmutableHashSet<IDependency> TopLevelDependencies { get; private set; }
            = ImmutableHashSet<IDependency>.Empty;

        public ImmutableDictionary<string, IDependency> DependenciesWorld { get; private set; }
            = ImmutableStringDictionary<IDependency>.EmptyOrdinalIgnoreCase;

        private readonly object _snapshotLock = new object();

        private Dictionary<string, IDependency> _topLevelDependenciesByPathMap
            = new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, IList<IDependency>> _dependenciesChildrenMap
            = new Dictionary<string, IList<IDependency>>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, bool> _unresolvedDescendantsMap
            = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private bool? _hasUresolvedDependency;
        public bool HasUnresolvedDependency
        {
            get
            {
                if (_hasUresolvedDependency == null)
                {
                    _hasUresolvedDependency = TopLevelDependencies.Any(x => !x.Resolved);
                    if (!_hasUresolvedDependency.Value)
                    {
                        _hasUresolvedDependency = DependenciesWorld.Values.Any(x => !x.Resolved);
                    }
                }

                return _hasUresolvedDependency.Value;
            }
        }

        public bool CheckForUnresolvedDependencies(IDependency dependency)
        {
            if (_unresolvedDescendantsMap.TryGetValue(dependency.Id, out bool hasUnresolvedDescendants))
            {
                return hasUnresolvedDescendants;
            }

            var seenDependencies = new HashSet<string>();

            return FindUnresolvedDependenciesRecursive(dependency, seenDependencies);
        }

        public bool CheckForUnresolvedDependencies(string providerType)
        {
            return DependenciesWorld.Values.Any(
                    x => x.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase) && !x.Resolved);
        }

        public IEnumerable<IDependency> GetDependencyChildren(IDependency dependency)
        {
            lock (_snapshotLock)
            {
                if (!_dependenciesChildrenMap.TryGetValue(dependency.Id, out IList<IDependency> children))
                {
                    children = new List<IDependency>();
                    foreach (var id in dependency.DependencyIDs)
                    {
                        if (TryToFindDependency(id, out IDependency child))
                        {
                            children.Add(child);
                        }
                    }

                    _dependenciesChildrenMap.Add(dependency.Id, children);
                }

                return children;
            }
        }

        private bool FindUnresolvedDependenciesRecursive(IDependency dependency, HashSet<string> seenDependencies)
        {

            var result = false;

            if (!seenDependencies.Contains(dependency.Id))
            {
                seenDependencies.Add(dependency.Id);
            }
            else
            {
                return result;
            }

            if (dependency.DependencyIDs.Count > 0)
            {
                foreach (var child in GetDependencyChildren(dependency))
                {
                    if (!child.Resolved)
                    {
                        result = true;
                        break;
                    }

                    if (!_unresolvedDescendantsMap.TryGetValue(child.Id, out bool depthFirstResult))
                    {
                        depthFirstResult = FindUnresolvedDependenciesRecursive(child, seenDependencies);
                    }

                    if (depthFirstResult)
                    {
                        result = true;
                        break;
                    }
                }
            }

            _unresolvedDescendantsMap[dependency.Id] = result;
            return result;
        }

        private bool TryToFindDependency(string id, out IDependency dependency)
        {
            if (DependenciesWorld.TryGetValue(id, out dependency))
            {
                return true;
            }

            return _topLevelDependenciesByPathMap.TryGetValue(id, out dependency);
        }

        private bool MergeChanges(
            IDependenciesChanges changes,
            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters,
            IEnumerable<IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs)
        {
            var worldBuilder = DependenciesWorld.ToBuilder();
            var topLevelBuilder = TopLevelDependencies.ToBuilder();

            var anyChanges = false;

            foreach (var removed in changes.RemovedNodes)
            {
                var targetedId = Dependency.GetID(TargetFramework, removed.ProviderType, removed.Id);
                if (!worldBuilder.TryGetValue(targetedId, out IDependency dependency))
                {
                    continue;
                }

                if (snapshotFilters != null)
                {
                    foreach (var filter in snapshotFilters)
                    {
                        dependency = filter.BeforeRemove(
                            ProjectPath, TargetFramework, dependency, worldBuilder, topLevelBuilder, out bool filterAnyChanges);

                        anyChanges |= filterAnyChanges;

                        if (dependency == null)
                        {
                            break;
                        }
                    }
                }

                if (dependency == null)
                {
                    continue;
                }

                anyChanges = true;

                worldBuilder.Remove(targetedId);
                topLevelBuilder.Remove(dependency);
            }

            var subTreeProvidersMap = GetSubTreeProviderMap(subTreeProviders);

            foreach (var added in changes.AddedNodes)
            {
                IDependency newDependency = new Dependency(added, TargetFramework, ProjectPath);

                if (snapshotFilters != null)
                {
                    foreach (var filter in snapshotFilters)
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

                worldBuilder.Remove(newDependency.Id);
                worldBuilder.Add(newDependency.Id, newDependency);
                if (newDependency.TopLevel)
                {
                    topLevelBuilder.Remove(newDependency);
                    topLevelBuilder.Add(newDependency);
                }
            }

            DependenciesWorld = worldBuilder.ToImmutable();
            TopLevelDependencies = topLevelBuilder.ToImmutable();

            ConstructTopLevelDependenciesByPathMap();

            return anyChanges;
        }

        private void ConstructTopLevelDependenciesByPathMap()
        {
            foreach (var topLevelDependency in TopLevelDependencies)
            {
                if (!string.IsNullOrEmpty(topLevelDependency.Path))
                {
                    _topLevelDependenciesByPathMap.Add(
                        Dependency.GetID(TargetFramework, topLevelDependency.ProviderType, topLevelDependency.Path),
                        topLevelDependency);
                }
            }
        }

        public static TargetedDependenciesSnapshot FromChanges(
            string projectPath,
            ITargetFramework targetFramework,
            ITargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters,
            IEnumerable<IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs,
            out bool anyChanges)
        {
            var newSnapshot = new TargetedDependenciesSnapshot(projectPath, targetFramework, previousSnapshot, catalogs);
            anyChanges = newSnapshot.MergeChanges(changes, snapshotFilters, subTreeProviders, projectItemSpecs);
            return newSnapshot;
        }

        private static Dictionary<string, IProjectDependenciesSubTreeProvider> GetSubTreeProviderMap(
            IEnumerable<IProjectDependenciesSubTreeProvider> subTreeProviders)
        {
            var subTreeProvidersMap = new Dictionary<string, IProjectDependenciesSubTreeProvider>(
                StringComparer.OrdinalIgnoreCase);
            if (subTreeProviders != null)
            {
                foreach (var provider in subTreeProviders)
                {
                    subTreeProvidersMap[provider.ProviderType] = provider;
                }
            }

            return subTreeProvidersMap;
        }
    }
}
