// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

/// <summary>
/// Implementations provide configured (per-slice) dependencies to the tree.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
internal interface IDependencySliceSubscriber
{
    /// <summary>
    /// Creates and returns a data source that produces snapshots of the dependency type(s) produced by this
    /// subscriber, scoped to the specified <paramref name="slice"/>.
    /// </summary>
    /// <param name="slice">The project slice within which to acquire configured dependencies.</param>
    /// <param name="source">A data source for the slice from which data may be obtained.</param>
    /// <returns>A data source that produces snapshots of the dependencies produced by this subscriber.</returns>
    IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>> Subscribe(
        ProjectConfigurationSlice slice,
        IActiveConfigurationSubscriptionSource source);
}
