// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Abstracts tree nodes API and allows to use them outside of <see cref="ProjectTreeProviderBase"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependenciesTreeServices
    {
        /// <summary>
        /// Creates <see cref="IProjectItemTree"/> - a tree node associated with a project item.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="itemContext"></param>
        /// <param name="propertySheet"></param>
        /// <param name="browseObjectProperties"></param>
        /// <param name="icon"></param>
        /// <param name="expandedIcon"></param>
        /// <param name="visible"></param>
        /// <param name="flags"></param>
        IProjectTree CreateTree(
            string caption,
            IProjectPropertiesContext itemContext,
            IPropertySheet? propertySheet = null,
            IRule? browseObjectProperties = null,
            ProjectImageMoniker? icon = null,
            ProjectImageMoniker? expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = null);

        /// <summary>
        /// Creates <see cref="IProjectTree"/> - a generic CPS tree node.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="filePath"></param>
        /// <param name="browseObjectProperties"></param>
        /// <param name="icon"></param>
        /// <param name="expandedIcon"></param>
        /// <param name="visible"></param>
        /// <param name="flags"></param>
        IProjectTree CreateTree(
            string caption,
            string? filePath,
            IRule? browseObjectProperties = null,
            ProjectImageMoniker? icon = null,
            ProjectImageMoniker? expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = null);

        /// <summary>
        /// Gets an <see cref="IRule"/> to attach to a project item, which would be used to
        /// display browse object properties page.
        /// </summary>
        /// <param name="dependency"></param>
        /// <param name="targetFramework"></param>
        /// <param name="catalogs"></param>
        Task<IRule?> GetBrowseObjectRuleAsync(
            IDependency dependency,
            TargetFramework targetFramework,
            IProjectCatalogSnapshot? catalogs);
    }
}
