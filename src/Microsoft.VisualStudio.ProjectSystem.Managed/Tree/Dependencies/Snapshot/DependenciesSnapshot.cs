// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    internal sealed class DependenciesSnapshot : IDependenciesSnapshot
    {
        #region Factories and private constructor

        public static DependenciesSnapshot CreateEmpty(string projectPath)
        {
            return new DependenciesSnapshot(
                projectPath,
                activeTargetFramework: TargetFramework.Empty,
                dependenciesByTargetFramework: ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);
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
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework? activeTargetFramework,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changes, nameof(changes));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            var builder = previousSnapshot.DependenciesByTargetFramework.ToBuilder();

            bool builderChanged = false;

            foreach ((ITargetFramework targetFramework, IDependenciesChanges dependenciesChanges) in changes)
            {
                if (!builder.TryGetValue(targetFramework, out ITargetedDependenciesSnapshot previousTargetedSnapshot))
                {
                    previousTargetedSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);
                }

                ITargetedDependenciesSnapshot newTargetedSnapshot = TargetedDependenciesSnapshot.FromChanges(
                    projectPath,
                    previousTargetedSnapshot,
                    dependenciesChanges,
                    catalogs,
                    snapshotFilters,
                    subTreeProviderByProviderType,
                    projectItemSpecs);

                if (!ReferenceEquals(previousTargetedSnapshot, newTargetedSnapshot))
                {
                    builder[targetFramework] = newTargetedSnapshot;
                    builderChanged = true;
                }
            }

            builderChanged |= SyncTargetFrameworks();

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

            bool SyncTargetFrameworks()
            {
                // Only sync if a the full list of target frameworks has been provided
                if (targetFrameworks.IsDefault)
                {
                    return false;
                }

                // This is a long-winded way of doing this that minimises allocations

                bool anythingRemoved = false;

                // Ensure all required target frameworks are present
                foreach (ITargetFramework targetFramework in targetFrameworks)
                {
                    if (!builder.ContainsKey(targetFramework))
                    {
                        builder.Add(targetFramework, TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs));
                        anythingRemoved = true;
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

                    anythingRemoved = true;
                }

                return anythingRemoved;
            }
        }

        public DependenciesSnapshot SetTargets(
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework activeTargetFramework)
        {
            bool activeChanged = !activeTargetFramework.Equals(ActiveTargetFramework);

            ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> map = DependenciesByTargetFramework;

            var diff = new SetDiff<ITargetFramework>(map.Keys, targetFrameworks);

            map = map.RemoveRange(diff.Removed);
            map = map.AddRange(
                diff.Added.Select(
                    added => new KeyValuePair<ITargetFramework, ITargetedDependenciesSnapshot>(
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
            ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> dependenciesByTargetFramework)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));
            Requires.NotNull(dependenciesByTargetFramework, nameof(dependenciesByTargetFramework));

            if (activeTargetFramework.Equals(TargetFramework.Empty))
            {
                Requires.Argument(
                    dependenciesByTargetFramework.Count == 0,
                    nameof(dependenciesByTargetFramework),
                    $"Must be empty when {nameof(activeTargetFramework)} is empty.");
            }
            else
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

        /// <inheritdoc />
        public string ProjectPath { get; }

        /// <inheritdoc />
        public ITargetFramework ActiveTargetFramework { get; }

        /// <inheritdoc />
        public ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> DependenciesByTargetFramework { get; }

        /// <inheritdoc />
        public bool HasVisibleUnresolvedDependency => DependenciesByTargetFramework.Any(x => x.Value.HasVisibleUnresolvedDependency);

        /// <inheritdoc />
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
                foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedDependencies) in DependenciesByTargetFramework)
                {
                    IDependency? dependency = targetedDependencies.TopLevelDependencies
                        .FirstOrDefault((x, id) => x.TopLevelIdEquals(id), dependencyId);

                    if (dependency != null)
                    {
                        return dependency;
                    }
                }
            }

            foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedDependencies) in DependenciesByTargetFramework)
            {
                if (targetedDependencies.DependenciesWorld.TryGetValue(dependencyId, out IDependency dependency))
                {
                    return dependency;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public bool Equals(IDependenciesSnapshot other)
        {
            return other != null && other.ProjectPath.Equals(ProjectPath, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => $"{DependenciesByTargetFramework.Count} target framework{(DependenciesByTargetFramework.Count == 1 ? "" : "s")} - {ProjectPath}";
    }
}
