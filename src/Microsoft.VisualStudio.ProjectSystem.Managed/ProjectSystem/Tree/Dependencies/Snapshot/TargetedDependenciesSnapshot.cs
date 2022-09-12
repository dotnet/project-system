// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
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
            IProjectCatalogSnapshot? catalogs)
        {
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));

            bool anyChanges = false;

            TargetFramework targetFramework = previousSnapshot.TargetFramework;

            var dependencyById = previousSnapshot.Dependencies.ToDictionary(IDependencyExtensions.GetDependencyId);

            if (changes is not null && changes.RemovedNodes.Count != 0)
            {
                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    dependencyById.Remove(removed.GetDependencyId());
                }

                anyChanges = true;
            }

            if (changes is not null && changes.AddedNodes.Count != 0)
            {
                foreach (IDependencyModel added in changes.AddedNodes)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    // NOTE we still need to check this in case extensions (eg. WebTools) provide us with top level items that need to be filtered out
                    if (!added.TopLevel)
                        continue;
#pragma warning restore CS0618 // Type or member is obsolete

                    IDependency dependency = new Dependency(added);

                    DeduplicateCaptions(ref dependency, dependencyById);

                    dependencyById[dependency.GetDependencyId()] = dependency;

                    anyChanges = true;
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
        }

        /// <summary>
        /// Deduplicates captions of top-level dependencies from the same provider. This is done by
        /// appending the <see cref="IDependencyModel.OriginalItemSpec"/> to the caption in parentheses.
        /// </summary>
        private static void DeduplicateCaptions(
            ref IDependency dependency,
            Dictionary<DependencyId, IDependency> dependencyById)
        {
            IDependency? matchingDependency = null;
            bool shouldApplyAlias = false;

            foreach ((DependencyId _, IDependency other) in dependencyById)
            {
                if (StringComparers.DependencyTreeIds.Equals(other.Id, dependency.Id) ||
                    !StringComparers.DependencyProviderTypes.Equals(other.ProviderType, dependency.ProviderType))
                {
                    continue;
                }

                if (other.Caption.StartsWith(dependency.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase))
                {
                    if (other.Caption.Length == dependency.Caption.Length)
                    {
                        // Exact match.
                        matchingDependency = other;
                        shouldApplyAlias = true;
                        break;
                    }

                    // Prefix matches.
                    // Check whether we have a match of form "Caption (Suffix)".
                    string? suffix = GetSuffix(other);

                    if (suffix is not null)
                    {
                        int expectedItemSpecIndex = dependency.Caption.Length + 2; // " (".Length
                        int expectedLength = expectedItemSpecIndex + suffix.Length + 1; // ")".Length

                        if (other.Caption.Length == expectedLength &&
                            string.Compare(other.Caption, expectedItemSpecIndex, suffix, 0, suffix.Length, StringComparisons.ProjectTreeCaptionIgnoreCase) == 0)
                        {
                            shouldApplyAlias = true;
                        }
                    }
                }
            }

            if (shouldApplyAlias)
            {
                if (matchingDependency is not null)
                {
                    // Change the matching dependency's caption too
                    IDependency modifiedMatching = matchingDependency.WithCaption(caption: GetAlias(matchingDependency));
                    dependencyById[modifiedMatching.GetDependencyId()] = modifiedMatching;
                }

                // Use the alias for the caption
                dependency = dependency.WithCaption(caption: GetAlias(dependency));
            }

            return;

            static string? GetSuffix(IDependency dependency) => dependency.OriginalItemSpec ?? dependency.FilePath;

            static string GetAlias(IDependency dependency)
            {
                string? suffix = GetSuffix(dependency);

                return Strings.IsNullOrEmpty(suffix) || suffix.Equals(dependency.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase)
                    ? dependency.Caption
                    : string.Concat(dependency.Caption, " (", suffix, ")");
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
