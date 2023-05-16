// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// Abstraction over selected <see cref="ProjectTreeProviderBase"/> methods, to assist with unit testing.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IProjectTreeOperations
{
    /// <summary>
    /// Gets any rule associated with the configured <see cref="IDependencyWithBrowseObject"/> dependency.
    /// </summary>
    /// <param name="dependency">The dependency to which the rule should apply.</param>
    /// <param name="configuredProject">The configured project within which the dependency exists.</param>
    /// <param name="catalogs">The collection of project catalogs to draw schema from.</param>
    /// <returns>A task that returns the rule, if found, otherwise <see langword="null"/>.</returns>
    ValueTask<IRule?> GetDependencyBrowseObjectRuleAsync(IDependencyWithBrowseObject dependency, ConfiguredProject? configuredProject, IProjectCatalogSnapshot? catalogs);

    /// <summary>
    /// Creates a tree node that represent a non-project item, and is not exposed via DTE.
    /// </summary>
    IProjectTree2 NewTree(string caption, string? filePath = null, IRule? browseObjectProperties = null, ProjectImageMoniker? icon = null, ProjectImageMoniker? expandedIcon = null, bool visible = true, ProjectTreeFlags? flags = null, int displayOrder = 0);

    /// <summary>
    /// Creates a tree node that represents a project item, and is exposed via DTE.
    /// </summary>
    IProjectItemTree2 NewTree(string caption, IProjectPropertiesContext item, IPropertySheet? propertySheet, IRule? browseObjectProperties = null, ProjectImageMoniker? icon = null, ProjectImageMoniker? expandedIcon = null, bool visible = true, ProjectTreeFlags? flags = null, bool isLinked = false, int displayOrder = 0);
}
