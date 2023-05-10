// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

public sealed class DependenciesSnapshotTests
{
    // TODO test MaximumDiagnosticLevel with some actual dependencies

    private readonly ProjectConfigurationSlice _slice = ProjectConfigurationSlice.Create(ImmutableDictionary<string, string>.Empty.Add("TargetFramework", "net8.0"));

    [Fact]
    public void Constructor()
    {
        var dependenciesSnapshotSlice = DependenciesSnapshotSliceFactory.Create(_slice);
        var dependenciesBySlice = ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice>.Empty.Add(_slice, dependenciesSnapshotSlice);
        var unconfiguredDependenciesByType = ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;

        var snapshot = new DependenciesSnapshot(_slice, dependenciesBySlice, unconfiguredDependenciesByType);

        Assert.Same(_slice, snapshot.PrimarySlice);
        Assert.Same(dependenciesBySlice, snapshot.DependenciesBySlice);
        Assert.Same(unconfiguredDependenciesByType, snapshot.UnconfiguredDependenciesByType);
        Assert.Equal(DiagnosticLevel.None, snapshot.MaximumDiagnosticLevel);
    }

    [Fact]
    public void Constructor_ThrowsIfPrimarySliceNotInDependenciesBySlice()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DependenciesSnapshot(
            primarySlice: _slice,
            dependenciesBySlice: ImmutableDictionary<ProjectConfigurationSlice, DependenciesSnapshotSlice>.Empty,
            unconfiguredDependenciesByType: ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty));

        Assert.StartsWith("Must contain the primary slice.", ex.Message);
    }
}
