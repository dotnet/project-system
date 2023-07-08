// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Immutable snapshot of a project's dependencies, both configured and unconfigured.
    /// </summary>
    /// <remarks>
    /// Only models top-level (direct) dependencies. No transitive dependencies are included.
    /// </remarks>
    internal sealed class DependenciesSnapshot
    {
        public DependenciesSnapshot(
            ProjectConfigurationSlice primarySlice,
            ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice> dependenciesBySlice,
            ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> unconfiguredDependenciesByType)
        {
            Requires.Argument(dependenciesBySlice.ContainsKey(primarySlice), nameof(dependenciesBySlice), "Must contain the primary slice.");

            PrimarySlice = primarySlice;
            DependenciesBySlice = dependenciesBySlice;
            UnconfiguredDependenciesByType = unconfiguredDependenciesByType;
        }

        /// <summary>
        /// Gets the slice representing the project's primary configuration.
        /// </summary>
        /// <remarks>
        /// When a project has multiple slices, the primary one is the first in the list.
        /// This information is used when creating the tree, where we only want one slice's
        /// dependencies to be made available via DTE/hierarchy APIs. We use the primary slice
        /// for consistency with other similar APIs.
        /// </remarks>
        public ProjectConfigurationSlice PrimarySlice { get; }

        /// <summary>
        /// Gets per-slice dependency snapshots.
        /// </summary>
        public ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice> DependenciesBySlice { get; }

        /// <summary>
        /// Gets unconfigured dependencies, by their dependency type.
        /// </summary>
        public ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> UnconfiguredDependenciesByType { get; }

        /// <summary>
        /// Gets the maximum diagnostic level across all dependencies within the snapshot.
        /// </summary>
        /// <remarks>
        /// This value determines which overlay, if any, to display on the root "Dependencies" node in the
        /// Solution Explorer, so that warnings and errors can be discovered even when the node is collapsed.
        /// </remarks>
        public DiagnosticLevel MaximumDiagnosticLevel
        {
            get
            {
                DiagnosticLevel max = UnconfiguredDependenciesByType.GetMaximumDiagnosticLevel();

                foreach ((_, DependenciesSnapshotSlice snapshotSlice) in DependenciesBySlice)
                {
                    if (snapshotSlice.MaximumDiagnosticLevel > max)
                    {
                        max = snapshotSlice.MaximumDiagnosticLevel;
                    }
                }

                return max;
            }
        }

        public override string ToString() => $"{DependenciesBySlice.Count} slice{(DependenciesBySlice.Count == 1 ? "" : "s")}";

        internal DependenciesSnapshot Update(
            ProjectConfigurationSlice primarySlice,
            ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice> configuredDependencies,
            ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> unconfiguredDependencies)
        {
            if (ReferenceEquals(primarySlice, PrimarySlice) &&
                ReferenceEquals(configuredDependencies, DependenciesBySlice) &&
                ReferenceEquals(unconfiguredDependencies, UnconfiguredDependenciesByType))
            {
                return this;
            }

            return new(primarySlice, configuredDependencies, unconfiguredDependencies);
        }

#if DEBUG
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is not DependenciesSnapshot other)
                return false;

            if (!Equals(PrimarySlice, other.PrimarySlice))
                return false;

            if (!CheckBySliceByGroup(DependenciesBySlice, other.DependenciesBySlice))
                return false;

            if (!CheckByGroup(UnconfiguredDependenciesByType, other.UnconfiguredDependenciesByType))
                return false;

            return true;

            static bool CheckBySliceByGroup(ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice> a, ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice> b)
            {
                if (a.Count != b.Count)
                    return false;
                if (!a.Keys.ToHashSet().SetEquals(b.Keys))
                    return false;

                foreach ((ProjectConfigurationSlice slice, DependenciesSnapshotSlice x) in a)
                {
                    DependenciesSnapshotSlice y = b[slice];

                    if (!Equals(x.Slice, y.Slice))
                        return false;
                    if (!Equals(x.ConfiguredProject, y.ConfiguredProject))
                        return false;
                    if (!Equals(x.Catalogs, y.Catalogs))
                        return false;

                    if (!CheckByGroup(x.DependenciesByType, y.DependenciesByType))
                        return false;
                }

                return true;
            }

            static bool CheckByGroup(ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> a, ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> b)
            {
                if (a.Count != b.Count)
                    return false;
                if (!a.Keys.ToHashSet().SetEquals(b.Keys))
                    return false;

                foreach ((DependencyGroupType groupType, ImmutableArray<IDependency> x) in a)
                {
                    ImmutableArray<IDependency> y = b[groupType];

                    if (!CheckDependencies(x, y))
                        return false;
                }

                return true;

                static bool CheckDependencies(ImmutableArray<IDependency> a, ImmutableArray<IDependency> b)
                {
                    if (a.Length != b.Length)
                        return false;

                    for (int i = 0; i < a.Length; i++)
                    {
                        IDependency x = a[i];
                        IDependency y = b[i];

                        if (!Equals(x, y))
                            return false;
                    }

                    return true;
                }
            }
        }

        public override int GetHashCode()
        {
            // Suppress warning, only in debug configuration.
            return base.GetHashCode();
        }
#endif
    }
}
