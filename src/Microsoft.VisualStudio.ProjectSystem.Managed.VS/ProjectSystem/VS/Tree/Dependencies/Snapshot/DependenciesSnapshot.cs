// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    internal class DependenciesSnapshot : IDependenciesSnapshot
    {
        #region Factories and private constructor

        public static DependenciesSnapshot CreateEmpty(string projectPath)
        {
            return new DependenciesSnapshot(
                projectPath,
                activeTarget: TargetFramework.Empty,
                targets: ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);
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
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string> projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changes, nameof(changes));
            // catalogs can be null
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));
            // projectItemSpecs can be null

            var builder = previousSnapshot.Targets.ToBuilder();

            bool targetChanged = false;

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
                    targetChanged = true;
                }
            }

            targetChanged |= RemoveTargetFrameworksWithNoDependencies();

            ITargetFramework activeTarget = activeTargetFramework ?? previousSnapshot.ActiveTarget;

            if (targetChanged)
            {
                // Targets have changed
                return new DependenciesSnapshot(
                    previousSnapshot.ProjectPath,
                    activeTarget,
                    builder.ToImmutable());
            }

            if (!activeTarget.Equals(previousSnapshot.ActiveTarget))
            {
                // The active target changed
                return new DependenciesSnapshot(
                    previousSnapshot.ProjectPath,
                    activeTarget,
                    previousSnapshot.Targets);
            }

            // Nothing has changed, so return the same snapshot
            return previousSnapshot;

            // Active target differs

            bool RemoveTargetFrameworksWithNoDependencies()
            {
                // This is a long-winded way of doing this that minimises allocations

                List<ITargetFramework> emptyFrameworks = null;
                bool anythingRemoved = false;

                foreach ((ITargetFramework targetFramework, ITargetedDependenciesSnapshot targetedSnapshot) in builder)
                {
                    if (targetedSnapshot.DependenciesWorld.Count == 0)
                    {
                        if (emptyFrameworks == null)
                        {
                            anythingRemoved = true;
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

                return anythingRemoved;
            }
        }

        public DependenciesSnapshot RemoveTargets(IEnumerable<ITargetFramework> targetToRemove)
        {
            ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> newTargets = Targets.RemoveRange(targetToRemove);

            // Return this if no targets changed
            return ReferenceEquals(newTargets, Targets)
                ? this
                : new DependenciesSnapshot(ProjectPath, ActiveTarget, newTargets);
        }

        private DependenciesSnapshot(
            string projectPath,
            ITargetFramework activeTarget,
            ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> targets)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(activeTarget, nameof(activeTarget));
            Requires.NotNull(targets, nameof(targets));

            ProjectPath = projectPath;
            ActiveTarget = activeTarget;
            Targets = targets;
        }

        #endregion

        /// <inheritdoc />
        public string ProjectPath { get; }

        /// <inheritdoc />
        public ITargetFramework ActiveTarget { get; }

        /// <inheritdoc />
        public ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> Targets { get; }

        /// <inheritdoc />
        public bool HasUnresolvedDependency => Targets.Any(x => x.Value.HasUnresolvedDependency);

        /// <inheritdoc />
        public IDependency FindDependency(string dependencyId, bool topLevel = false)
        {
            if (string.IsNullOrEmpty(dependencyId))
            {
                return null;
            }

            if (topLevel)
            {
                // if top level first try to find by top level id with full path,
                // if found - return, if not - try regular Id in the DependenciesWorld
                foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedDependencies) in Targets)
                {
                    IDependency dependency = targetedDependencies.TopLevelDependencies
                        .FirstOrDefault((x, id) => x.TopLevelIdEquals(id), dependencyId);

                    if (dependency != null)
                    {
                        return dependency;
                    }
                }
            }

            foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedDependencies) in Targets)
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
    }
}
