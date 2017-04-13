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
            = ImmutableDictionary<string, IDependency>.Empty;

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

        private Dictionary<string, bool> _unresolvedDescendantsMap 
            = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
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
                    x => x.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase) && !x.Resolved);
        }

        private bool FindUnresolvedDependenciesRecursive(IDependency dependency)
        {
            var result = false;
            if (dependency.DependencyIDs.Count > 0)
            {
                foreach (var child in dependency.Dependencies)
                {
                    if (!child.Resolved)
                    {
                        result = true;
                        break;
                    }

                    if (!_unresolvedDescendantsMap.TryGetValue(child.Id, out bool depthFirstResult))
                    {
                        depthFirstResult = FindUnresolvedDependenciesRecursive(child);
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

        private bool MergeChanges(
            IDependenciesChanges changes, 
            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters)
        {
            var topLevelBuilder = TopLevelDependencies.ToBuilder();
            var worldBuilder = ImmutableDictionary.CreateBuilder<string, IDependency>(
                                    StringComparer.OrdinalIgnoreCase);
            worldBuilder.AddRange(DependenciesWorld);
            var anyChanges = false;

            foreach (var removed in changes.RemovedNodes)
            {
                var targetedId = Dependency.GetID(TargetFramework, removed.ProviderType, removed.Id);
                if (!worldBuilder.TryGetValue(targetedId, out IDependency dependency))
                {
                    continue;
                }

                snapshotFilters.ForEach(
                    filter => filter.BeforeRemove(ProjectPath, TargetFramework, dependency, worldBuilder, topLevelBuilder));

                anyChanges = true;

                worldBuilder.Remove(targetedId);
                topLevelBuilder.Remove(dependency);             
            }

            foreach (var added in changes.AddedNodes)
            {
                IDependency newDependency = new Dependency(added, this);
                
                foreach(var filter in snapshotFilters)
                {
                    newDependency = filter.BeforeAdd(ProjectPath, TargetFramework, newDependency, worldBuilder, topLevelBuilder);
                    if (newDependency == null)
                    {
                        break;
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

            return anyChanges;
        }

        public static TargetedDependenciesSnapshot FromChanges(
            string projectPath,
            ITargetFramework targetFramework,
            ITargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters,
            out bool anyChanges)
        {
            var newSnapshot = new TargetedDependenciesSnapshot(projectPath, targetFramework, previousSnapshot, catalogs);
            anyChanges = newSnapshot.MergeChanges(changes, snapshotFilters);
            return newSnapshot;
        }
    }
}
