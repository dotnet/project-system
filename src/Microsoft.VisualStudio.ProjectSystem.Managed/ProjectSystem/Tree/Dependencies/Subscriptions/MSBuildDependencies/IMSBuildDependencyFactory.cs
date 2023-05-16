// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

/// <summary>
/// Exports of this interface contribute dependencies from MSBuild items.
/// </summary>
/// <remarks>
/// <para>
/// Dependencies from MSBuild use items from both evaluation and design-time build targets.
/// The latter are fully resolved with all item metadata, while the former contain just the information found in the
/// project file, which is enough to quickly populate the dependency tree while we wait for the slower design-time
/// build to complete and return richer item metadata.
/// </para>
/// <para>
/// Implementations should derive from <see cref="MSBuildDependencyFactoryBase"/>.
/// </para>
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
internal interface IMSBuildDependencyFactory
{
    /// <summary>
    /// Gets the rule name for dependency items returned via evaluation (eg: <c>PackageReference</c>).
    /// </summary>
    string UnresolvedRuleName { get; }

    /// <summary>
    /// Gets the rule name for dependency items resolved by design-time builds (eg: <c>ResolvedPackageReference</c>).
    /// </summary>
    string ResolvedRuleName { get; }

    /// <summary>
    /// Creates a new collection for MSBuild dependencies of this object's type.
    /// The returned collection understands how to integrate project updates in order to keep its dependencies up to date.
    /// </summary>
    /// <remarks>
    /// We have one such collection per dependency type, per project, per slice.
    /// </remarks>
    MSBuildDependencyCollection CreateCollection();
}
