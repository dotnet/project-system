// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

public sealed class DependenciesSnapshotSliceTests
{
    // TODO test MaximumDiagnosticLevel
    // TODO test GetMaximumDiagnosticLevelForDependencyGroupType

    private readonly IProjectCatalogSnapshot _catalogs = IProjectCatalogSnapshotFactory.Create();
    private readonly ConfiguredProject _configuredProject = ConfiguredProjectFactory.Create();

    private readonly ProjectConfigurationSlice _slice1 = ProjectConfigurationSlice.Create(ImmutableDictionary<string, string>.Empty.Add("TargetFramework", "tfm1"));

    [Fact]
    public void Update()
    {
        var dependenciesByType = ImmutableArray<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>.Empty;

        DependenciesSnapshotSlice? snapshot = null;

        DependenciesSnapshotSlice.Update(
            ref snapshot,
            _slice1,
            _configuredProject,
            _catalogs,
            dependenciesByType);

        Assert.Same(_slice1, snapshot.Slice);
        Assert.Empty(snapshot.DependenciesByType);
        Assert.Same(_configuredProject, snapshot.ConfiguredProject);
        Assert.Same(_catalogs, snapshot.Catalogs);
        Assert.Equal(DiagnosticLevel.None, snapshot.MaximumDiagnosticLevel);
    }
}
