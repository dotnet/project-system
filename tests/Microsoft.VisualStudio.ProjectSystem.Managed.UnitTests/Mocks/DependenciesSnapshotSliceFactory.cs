// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

internal static class DependenciesSnapshotSliceFactory
{
    public static DependenciesSnapshotSlice Create(ProjectConfigurationSlice slice, ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>? dependenciesByType = null)
    {
        dependenciesByType ??= ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;

        DependenciesSnapshotSlice ? dependenciesSnapshotSlice = null;

        DependenciesSnapshotSlice.Update(
            ref dependenciesSnapshotSlice,
            slice,
            ConfiguredProjectFactory.Create(),
            IProjectCatalogSnapshotFactory.Create(),
            ImmutableArray.Create(dependenciesByType));

        return dependenciesSnapshotSlice;
    }
}
