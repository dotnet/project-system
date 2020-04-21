// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Immutable snapshot of all project dependencies across all target frameworks.
    /// </summary>
    internal sealed class DependenciesSnapshot
    {
        #region Factories and private constructor

        public static DependenciesSnapshot CreateEmpty(string projectPath)
        {
            return new DependenciesSnapshot(
                projectPath,
                activeTargetFramework: TargetFramework.Empty,
                dependenciesByTargetFramework: ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot>.Empty);
        }

        /// <summary>
        /// For each target framework in <paramref name="changes"/>, applies the corresponding
        /// <see cref="IDependenciesChanges"/> to <paramref name="previousSnapshot"/> in order to produce
        /// and return an updated <see cref="DependenciesSnapshot"/> object.
        /// If no changes are made, <paramref name="previousSnapshot"/> is returned unmodified.
        /// </summary>
        /// <remarks>
        /// As part of the update, each <see cref="IDependenciesSnapshotFilter"/> in <paramref name="snapshotFilters"/>
        /// is given a chance to influence the addition and removal of dependency data in the returned snapshot.
        /// </remarks>
        /// <returns>An updated snapshot, or <paramref name="previousSnapshot"/> if no changes occured.</returns>
        public static DependenciesSnapshot FromChanges(
            string projectPath,
            DependenciesSnapshot previousSnapshot,
            ITargetFramework changedTargetFramework,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework? activeTargetFramework,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changedTargetFramework, nameof(changedTargetFramework));
            Requires.NotNull(changes, nameof(changes));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            var builder = previousSnapshot.DependenciesByTargetFramework.ToBuilder();

            if (!builder.TryGetValue(changedTargetFramework, out TargetedDependenciesSnapshot previousTargetedSnapshot))
            {
                previousTargetedSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, changedTargetFramework, catalogs);
            }

            bool builderChanged = false;

            var newTargetedSnapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousTargetedSnapshot,
                changes,
                catalogs,
                snapshotFilters,
                subTreeProviderByProviderType,
                projectItemSpecs);

            if (!ReferenceEquals(previousTargetedSnapshot, newTargetedSnapshot))
            {
                builder[changedTargetFramework] = newTargetedSnapshot;
                builderChanged = true;
            }

            SyncTargetFrameworks();

            activeTargetFramework ??= previousSnapshot.ActiveTargetFramework;

            if (builderChanged)
            {
                // Dependencies-by-target-framework has changed
                return new DependenciesSnapshot(
                    projectPath,
                    activeTargetFramework,
                    builder.ToImmutable());
            }

            if (!activeTargetFramework.Equals(previousSnapshot.ActiveTargetFramework))
            {
                // The active target framework changed
                return new DependenciesSnapshot(
                    projectPath,
                    activeTargetFramework,
                    previousSnapshot.DependenciesByTargetFramework);
            }

            if (projectPath != previousSnapshot.ProjectPath)
            {
                // The project path changed
                return new DependenciesSnapshot(
                    projectPath,
                    activeTargetFramework,
                    previousSnapshot.DependenciesByTargetFramework);
            }

            // Nothing has changed, so return the same snapshot
            return previousSnapshot;

            void SyncTargetFrameworks()
            {
                // Only sync if a the full list of target frameworks has been provided
                if (targetFrameworks.IsDefault)
                {
                    return;
                }

                // This is a long-winded way of doing this that minimises allocations

                // Ensure all required target frameworks are present
                foreach (ITargetFramework targetFramework in targetFrameworks)
                {
                    if (!builder.ContainsKey(targetFramework))
                    {
                        builder.Add(targetFramework, TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs));
                        builderChanged = true;
                    }
                }

                // Remove any extra target frameworks
                if (builder.Count != targetFrameworks.Length)
                {
                    IEnumerable<ITargetFramework> targetFrameworksToRemove = builder.Keys.Except(targetFrameworks);

                    foreach (ITargetFramework targetFramework in targetFrameworksToRemove)
                    {
                        builder.Remove(targetFramework);
                    }

                    builderChanged = true;
                }
            }
        }

        public DependenciesSnapshot SetTargets(
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework activeTargetFramework)
        {
            bool activeChanged = !activeTargetFramework.Equals(ActiveTargetFramework);

            ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> map = DependenciesByTargetFramework;

            var diff = new SetDiff<ITargetFramework>(map.Keys, targetFrameworks);

            map = map.RemoveRange(diff.Removed);
            map = map.AddRange(
                diff.Added.Select(
                    added => new KeyValuePair<ITargetFramework, TargetedDependenciesSnapshot>(
                        added,
                        TargetedDependenciesSnapshot.CreateEmpty(ProjectPath, added, null))));

            if (activeChanged || !ReferenceEquals(map, DependenciesByTargetFramework))
            {
                return new DependenciesSnapshot(ProjectPath, activeTargetFramework, map);
            }

            return this;
        }

        // Internal, for test use -- normal code should use the factory methods
        internal DependenciesSnapshot(
            string projectPath,
            ITargetFramework activeTargetFramework,
            ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> dependenciesByTargetFramework)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));
            Requires.NotNull(dependenciesByTargetFramework, nameof(dependenciesByTargetFramework));

            if (!activeTargetFramework.Equals(TargetFramework.Empty))
            {
                Requires.Argument(
                    dependenciesByTargetFramework.ContainsKey(activeTargetFramework),
                    nameof(dependenciesByTargetFramework),
                    $"Must contain {nameof(activeTargetFramework)} ({activeTargetFramework.FullName}).");
            }

            ProjectPath = projectPath;
            ActiveTargetFramework = activeTargetFramework;
            DependenciesByTargetFramework = dependenciesByTargetFramework;
        }

        #endregion

        /// <summary>
        /// Gets the full path to the project file whose dependencies this snapshot contains.
        /// </summary>
        /// <remarks>
        /// Cannot be null or empty.
        /// </remarks>
        public string ProjectPath { get; }

        /// <summary>
        /// Gets the active target framework for project.
        /// </summary>
        public ITargetFramework ActiveTargetFramework { get; }

        /// <summary>
        /// Gets a dictionary of dependencies by target framework.
        /// </summary>
        public ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> DependenciesByTargetFramework { get; }

        /// <summary>
        /// Gets whether this snapshot contains at least one unresolved dependency which is both visible
        /// and reachable from a visible top-level dependency, for any target framework.
        /// </summary>
        public bool HasReachableVisibleUnresolvedDependency => DependenciesByTargetFramework.Any(x => x.Value.HasReachableVisibleUnresolvedDependency);

        /// <summary>
        /// Finds dependency for given id across all target frameworks.
        /// </summary>
        /// <param name="dependencyId">Unique id for dependency to be found.</param>
        /// <param name="topLevel">If <see langword="true"/>, search is first performed on top level
        /// dependencies before searching all dependencies.</param>
        /// <returns>The <see cref="IDependency"/> if found, otherwise <see langword="null"/>.</returns>
        public IDependency? FindDependency(string dependencyId, bool topLevel = false)
        {
            if (string.IsNullOrEmpty(dependencyId))
            {
                return null;
            }

            if (topLevel)
            {
                // if top level first try to find by top level id with full path,
                // if found - return, if not - try regular Id in the DependenciesWorld
                foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedDependencies) in DependenciesByTargetFramework)
                {
                    IDependency? dependency = targetedDependencies.TopLevelDependencies
                        .FirstOrDefault((x, id) => x.TopLevelIdEquals(id), dependencyId);

                    if (dependency != null)
                    {
                        return dependency;
                    }
                }
            }

            foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedDependencies) in DependenciesByTargetFramework)
            {
                if (targetedDependencies.DependenciesWorld.TryGetValue(dependencyId, out IDependency dependency))
                {
                    return dependency;
                }
            }

            return null;
        }

        public override string ToString() => $"{DependenciesByTargetFramework.Count} target framework{(DependenciesByTargetFramework.Count == 1 ? "" : "s")} - {ProjectPath}";
    }
}
