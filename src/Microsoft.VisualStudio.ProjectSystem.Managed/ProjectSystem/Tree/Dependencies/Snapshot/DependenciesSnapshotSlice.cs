// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

/// <summary>
/// Models the dependencies of a project, within a single configuration slice.
/// </summary>
internal sealed class DependenciesSnapshotSlice
{
    public static void Update(
        [NotNull] ref DependenciesSnapshotSlice? snapshot,
        ProjectConfigurationSlice slice,
        ConfiguredProject configuredProject,
        IProjectCatalogSnapshot catalogs,
        IReadOnlyList<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> sourceUpdates)
    {
        ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> dependenciesByType;

        if (snapshot is null)
        {
            dependenciesByType = MergeUpdates();
        }
        else
        {
            Assumes.True(ReferenceEquals(slice, snapshot.Slice));

            dependenciesByType = SyncUpdates(snapshot);

            if (ReferenceEquals(configuredProject, snapshot.ConfiguredProject) &&
                ReferenceEquals(catalogs, snapshot.Catalogs) &&
                ReferenceEquals(dependenciesByType, snapshot.DependenciesByType))
            {
                return;
            }
        }

        snapshot = new(
            slice,
            dependenciesByType,
            sourceUpdates,
            configuredProject,
            catalogs);

        ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> SyncUpdates(DependenciesSnapshotSlice snapshot)
        {
            if (SameSources())
            {
                return snapshot.DependenciesByType;
            }
            else
            {
                return MergeUpdates();
            }

            bool SameSources()
            {
                if (sourceUpdates.Count != snapshot._sourceUpdates.Count)
                {
                    return false;
                }

                for (int i = 0; i < sourceUpdates.Count; i++)
                {
                    if (!ReferenceEquals(sourceUpdates[i], snapshot._sourceUpdates[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> MergeUpdates()
        {
            // Multiple IDependencySliceSubscriber exports may contribute dependencies of the same DependencyGroupType, so we walk through and merge them here.

            Dictionary<DependencyGroupType, List<IDependency>> dependenciesByType = new();

            foreach (ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> dependencySliceUpdate in sourceUpdates)
            {
                foreach ((DependencyGroupType type, ImmutableArray<IDependency> dependenciesToAdd) in dependencySliceUpdate)
                {
                    if (!dependenciesByType.TryGetValue(type, out List<IDependency>? dependencies))
                    {
                        dependenciesByType.Add(type, dependencies = new());
                    }

                    dependencies.AddRange(dependenciesToAdd);
                }
            }

            return dependenciesByType.ToImmutableDictionary(
                static pair => pair.Key,
                static pair => pair.Value.ToImmutableArray());
        }
    }

    private readonly IReadOnlyList<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> _sourceUpdates;

    private DependenciesSnapshotSlice(
        ProjectConfigurationSlice slice,
        ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> dependenciesByType,
        IReadOnlyList<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> sourceUpdates,
        ConfiguredProject configuredProject,
        IProjectCatalogSnapshot catalogs)
    {
        Slice = slice;
        DependenciesByType = dependenciesByType;
        _sourceUpdates = sourceUpdates;
        ConfiguredProject = configuredProject;
        Catalogs = catalogs;

        MaximumDiagnosticLevel = dependenciesByType.GetMaximumDiagnosticLevel();
    }

    /// <summary>
    /// The <see cref="ProjectConfigurationSlice" /> associated with the dependencies in this snapshot.
    /// </summary>
    /// <remarks>
    /// If there is only one slice in the project, this will be empty.
    /// </remarks>
    public ProjectConfigurationSlice Slice { get; }

    /// <summary>
    /// Contains all <see cref="IDependency"/> objects in the project for the given slice, grouped by <see cref="DependencyGroupType"/>.
    /// </summary>
    public ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> DependenciesByType { get; }

    /// <summary>
    /// Gets the active configured project for this slice. This value will change in response to configuration changes.
    /// </summary>
    public ConfiguredProject ConfiguredProject { get; }

    /// <summary>
    /// Gets the set of rule/schema catalogs for the project.
    /// </summary>
    public IProjectCatalogSnapshot Catalogs { get; }

    /// <summary>
    /// Gets the most severe diagnostic level among the dependencies in this snapshot.
    /// </summary>
    public DiagnosticLevel MaximumDiagnosticLevel { get; }

    /// <summary>
    /// Returns the most severe <see cref="DiagnosticLevel"/> for the dependencies in this snapshot belonging to
    /// the specified <see cref="DependencyGroupType"/>.
    /// </summary>
    public DiagnosticLevel GetMaximumDiagnosticLevelForDependencyGroupType(DependencyGroupType dependencyType)
    {
        if (MaximumDiagnosticLevel == DiagnosticLevel.None)
        {
            // No item in the snapshot has a diagnostic, so the result must be 'None'
            return DiagnosticLevel.None;
        }

        DiagnosticLevel max = DiagnosticLevel.None;

        if (DependenciesByType.TryGetValue(dependencyType, out ImmutableArray<IDependency> dependencies))
        {
            foreach (IDependency dependency in dependencies)
            {
                if (dependency.DiagnosticLevel > max)
                {
                    max = dependency.DiagnosticLevel;
                }
            }
        }

        return max;
    }

    public override string ToString() => $"{Slice} - {DependenciesByType.Count} dependency types, {DependenciesByType.Sum(pair => pair.Value.Length)} dependencies";
}
