// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal sealed class TargetedDependenciesSnapshot
    {
        #region Factories and internal constructor

        public static TargetedDependenciesSnapshot CreateEmpty(TargetFramework targetFramework, IProjectCatalogSnapshot? catalogs)
        {
            return new TargetedDependenciesSnapshot(
                targetFramework,
                catalogs,
                ImmutableArray<IDependency>.Empty);
        }

        /// <summary>
        /// Applies changes to <paramref name="previousSnapshot"/> and produces a new snapshot if required.
        /// If no changes are made, <paramref name="previousSnapshot"/> is returned unmodified.
        /// </summary>
        /// <returns>An updated snapshot, or <paramref name="previousSnapshot"/> if no changes occurred.</returns>
        public static TargetedDependenciesSnapshot FromChanges(
            TargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges? changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters)
        {
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");

            bool anyChanges = false;

            TargetFramework targetFramework = previousSnapshot.TargetFramework;

            var dependencyById = previousSnapshot.Dependencies.ToDictionary(IDependencyExtensions.GetDependencyId);

            if (changes != null && changes.RemovedNodes.Count != 0)
            {
                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    dependencyById.Remove(removed.GetDependencyId());
                }

                anyChanges = true;
            }

            if (changes != null && changes.AddedNodes.Count != 0)
            {
                var context = new AddDependencyContext(dependencyById);

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
                !targetFramework.Equals(previousSnapshot.TargetFramework) ||
                !Equals(catalogs, previousSnapshot.Catalogs);

            if (anyChanges)
            {
                return new TargetedDependenciesSnapshot(
                    targetFramework,
                    catalogs,
                    dependencyById.ToImmutableValueArray());
            }

            return previousSnapshot;

            void Add(AddDependencyContext context, IDependencyModel dependencyModel)
            {
                // Create the unfiltered dependency
                IDependency? dependency = new Dependency(dependencyModel);

                context.Reset();

                foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                {
                    filter.BeforeAddOrUpdate(
                        dependency,
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
                    DependencyId id = dependencyModel.GetDependencyId();
                    dependencyById.Remove(id);
                    dependencyById.Add(id, dependency);
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
            TargetFramework targetFramework,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<IDependency> dependencies)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.Argument(!dependencies.IsDefault, nameof(dependencies), "Cannot be default.");

            TargetFramework = targetFramework;
            Catalogs = catalogs;
            Dependencies = dependencies;

            MaximumVisibleDiagnosticLevel = GetMaximumVisibleDiagnosticLevel();

            DiagnosticLevel GetMaximumVisibleDiagnosticLevel()
            {
                DiagnosticLevel max = DiagnosticLevel.None;

                foreach (IDependency dependency in Dependencies)
                {
                    if (dependency.Visible && dependency.DiagnosticLevel > max)
                    {
                        max = dependency.DiagnosticLevel;
                    }
                }

                return max;
            }
        }

        #endregion

        /// <summary>
        /// <see cref="TargetFramework" /> for which project has dependencies contained in this snapshot.
        /// </summary>
        public TargetFramework TargetFramework { get; }

        /// <summary>
        /// Catalogs of rules for project items (optional, custom dependency providers might not provide it).
        /// </summary>
        public IProjectCatalogSnapshot? Catalogs { get; }

        /// <summary>
        /// Contains all <see cref="IDependency"/> objects in the project for the given target.
        /// </summary>
        public ImmutableArray<IDependency> Dependencies { get; }

        /// <summary>
        /// Gets the most severe diagnostic level among the dependencies in this snapshot.
        /// </summary>
        public DiagnosticLevel MaximumVisibleDiagnosticLevel { get; }

        /// <summary>
        /// Returns the most severe <see cref="DiagnosticLevel"/> for the dependencies in this snapshot belonging to
        /// the specified <paramref name="providerType"/>.
        /// </summary>
        public DiagnosticLevel GetMaximumVisibleDiagnosticLevelForProvider(string providerType)
        {
            if (MaximumVisibleDiagnosticLevel == DiagnosticLevel.None)
            {
                // No item in the snapshot has a diagnostic, so the result must be 'None'
                return DiagnosticLevel.None;
            }

            DiagnosticLevel max = DiagnosticLevel.None;

            foreach (IDependency dependency in Dependencies)
            {
                if (dependency.Visible && StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, providerType))
                {
                    if (dependency.DiagnosticLevel > max)
                    {
                        max = dependency.DiagnosticLevel;
                    }
                }
            }

            return max;
        }

        public override string ToString() => $"{TargetFramework.TargetFrameworkAlias} - {Dependencies.Length} dependencies";
    }
}
