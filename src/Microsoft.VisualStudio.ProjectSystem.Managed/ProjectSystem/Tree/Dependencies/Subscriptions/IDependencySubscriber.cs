// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

/// <summary>
/// Implementations provide unconfigured dependencies to the tree.
/// </summary>
/// <remarks>
/// Unconfigured dependencies are those that do not belong to any specific project configuration, and would
/// usually be sourced from outside MSBuild where the concept of configuration does not apply.
/// For example, for JavaScript/TypeScript projects, NPM packages exist independently of any specific
/// configuration.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
internal interface IDependencySubscriber
{
    /// <summary>
    /// Creates and returns a data source for the type(s) of dependencies produced by this subscriber.
    /// If no dependencies will be produced, this method may return <see langword="null"/>.
    /// </summary>
    IProjectValueDataSource<ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>>? Subscribe();
}
