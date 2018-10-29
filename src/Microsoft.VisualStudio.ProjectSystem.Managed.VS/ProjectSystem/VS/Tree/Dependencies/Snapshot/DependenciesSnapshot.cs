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
    /// <inheritdoc />
    internal class DependenciesSnapshot : IDependenciesSnapshot
    {
        public static DependenciesSnapshot CreateEmpty(string projectPath)
        {
            return new DependenciesSnapshot(projectPath);
        }

        public static DependenciesSnapshot FromChanges(
            string projectPath,
            DependenciesSnapshot previousSnapshot,
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework,
            IReadOnlyCollection<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyCollection<IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs,
            out bool anyChanges)
        {
            var newSnapshot = new DependenciesSnapshot(projectPath, activeTargetFramework, previousSnapshot);
            anyChanges = newSnapshot.MergeChanges(changes, catalogs, snapshotFilters, subTreeProviders, projectItemSpecs);
            return newSnapshot;
        }

        public DependenciesSnapshot RemoveTargets(IEnumerable<ITargetFramework> targetToRemove)
        {
            var newSnapshot = new DependenciesSnapshot(ProjectPath, ActiveTarget, this);
            newSnapshot.Targets = newSnapshot.Targets.RemoveRange(targetToRemove);
            return newSnapshot;
        }

        private DependenciesSnapshot(
            string projectPath,
            ITargetFramework activeTarget = null,
            DependenciesSnapshot previousSnapshot = null)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));

            ProjectPath = projectPath;
            ActiveTarget = activeTarget;

            if (previousSnapshot != null)
            {
                _targets = previousSnapshot._targets;
                if (ActiveTarget == null)
                {
                    ActiveTarget = previousSnapshot.ActiveTarget;
                }
            }

            if (ActiveTarget == null)
            {
                ActiveTarget = TargetFramework.Empty;
            }
        }

        /// <inheritdoc />
        public string ProjectPath { get; }

        /// <inheritdoc />
        public ITargetFramework ActiveTarget { get; }

        private ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> _targets =
            ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty;

        /// <inheritdoc />
        public IImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> Targets
        {
            get => _targets;
            private set => _targets = value.ToImmutableDictionary();
        }

        /// <inheritdoc />
        public bool HasUnresolvedDependency => Targets.Any(x => x.Value.HasUnresolvedDependency);

        /// <inheritdoc />
        public IDependency FindDependency(string id, bool topLevel = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (topLevel)
            {
                // if top level first try to find by top level id with full path,
                // if found - return, if not - try regular Id in the DependenciesWorld
                foreach (ITargetedDependenciesSnapshot targetedDependencies in Targets.Values)
                {
                    IDependency dependency = targetedDependencies.TopLevelDependencies.FirstOrDefault(
                        x => x.TopLevelIdEquals(id));

                    if (dependency != null)
                    {
                        return dependency;
                    }
                }
            }

            foreach (ITargetedDependenciesSnapshot targetedDependencies in Targets.Values)
            {
                if (targetedDependencies.DependenciesWorld.TryGetValue(id, out IDependency dependency))
                {
                    return dependency;
                }
            }

            return null;
        }

        private bool MergeChanges(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            IReadOnlyCollection<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyCollection<IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs)
        {
            bool anyChanges = false;
            var builder = _targets.ToBuilder();

            foreach ((ITargetFramework targetFramework, IDependenciesChanges dependenciesChanges) in changes)
            {
                builder.TryGetValue(targetFramework, out ITargetedDependenciesSnapshot previousSnapshot);

                var newTargetedSnapshot = TargetedDependenciesSnapshot.FromChanges(
                    ProjectPath,
                    targetFramework,
                    previousSnapshot,
                    dependenciesChanges,
                    catalogs,
                    snapshotFilters,
                    subTreeProviders,
                    projectItemSpecs,
                    out bool anyTfmChanges);

                if (anyTfmChanges)
                {
                    builder[targetFramework] = newTargetedSnapshot;
                    anyChanges = true;
                }
            }

            RemoveTargetFrameworksWithNoDependencies();

            if (anyChanges)
            {
                _targets = builder.ToImmutableDictionary();
            }

            return anyChanges;

            void RemoveTargetFrameworksWithNoDependencies()
            {
                // This is a long-winded way of doing this that minimises allocations

                List<ITargetFramework> emptyFrameworks = null;

                foreach ((ITargetFramework targetFramework, ITargetedDependenciesSnapshot targetedSnapshot) in builder)
                {
                    if (targetedSnapshot.DependenciesWorld.Count == 0)
                    {
                        if (emptyFrameworks == null)
                        {
                            anyChanges = true;
                            emptyFrameworks = new List<ITargetFramework>(builder.Count);
                        }

                        emptyFrameworks.Add(targetFramework);
                    }
                }

                if (emptyFrameworks != null)
                {
                    foreach (ITargetFramework framework in emptyFrameworks)
                    {
                        builder.Remove(framework);
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool Equals(IDependenciesSnapshot other)
        {
            return other != null && other.ProjectPath.Equals(ProjectPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
