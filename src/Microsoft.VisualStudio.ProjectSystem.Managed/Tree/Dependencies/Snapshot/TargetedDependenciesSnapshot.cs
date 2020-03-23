// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            IDependenciesChanges? changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            bool anyChanges = false;

            ITargetFramework targetFramework = previousSnapshot.TargetFramework;

            var builder = previousSnapshot.DependencyById.ToBuilder();

            if (changes != null && changes.RemovedNodes.Count != 0)
            {
                var context = new RemoveDependencyContext(builder);

                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    Remove(context, removed);
                }
            }

            if (changes != null && changes.AddedNodes.Count != 0)
            {
                var context = new AddDependencyContext(builder);

                foreach (IDependencyModel added in changes.AddedNodes)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    // NOTE we still need to check this in case extensions (eg. WebTools) provide us with top level items that need to be filtered out
                    if (!added.TopLevel)
                        continue;
#pragma warning restore CS0618 // Type or member is obsolete

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
                    builder.ToImmutable());
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

                builder.Remove(dependencyId);
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
                    builder.Remove(dependency.Id);
                    builder.Add(dependency.Id, dependency);
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
            ImmutableDictionary<string, IDependency> dependencyById)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNull(dependencyById, nameof(dependencyById));
            Assumes.True(Equals(dependencyById.KeyComparer, StringComparers.DependencyTreeIds), $"{nameof(dependencyById)} must have an {nameof(StringComparers.DependencyTreeIds)} key comparer.");

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;
            DependencyById = dependencyById;

            HasVisibleUnresolvedDependency = dependencyById.Any(pair => pair.Value.Visible && !pair.Value.Resolved);
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
        /// Contains all unique <see cref="IDependency"/> objects in the project, from all levels.
        /// Allows looking them up by their IDs.
        /// </summary>
        public ImmutableDictionary<string, IDependency> DependencyById { get; }

        /// <summary>
        /// Gets whether this snapshot contains at least one visible unresolved dependency.
        /// </summary>
        public bool HasVisibleUnresolvedDependency { get; }

        /// <summary>
        /// Efficient API for checking if a there is at least one unresolved dependency with given provider type.
        /// </summary>
        /// <param name="providerType">Provider type to check</param>
        /// <returns>Returns true if there is at least one unresolved dependency with given providerType.</returns>
        public bool CheckForUnresolvedDependencies(string providerType)
        {
            if (HasVisibleUnresolvedDependency == false)
            {
                return false;
            }

            foreach ((_, IDependency dependency) in DependencyById)
            {
                if (!dependency.Resolved &&
                    dependency.Visible &&
                    StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, providerType))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString() => $"{TargetFramework.FriendlyName} - {DependencyById.Count} dependencies - {ProjectPath}";
    }
}
