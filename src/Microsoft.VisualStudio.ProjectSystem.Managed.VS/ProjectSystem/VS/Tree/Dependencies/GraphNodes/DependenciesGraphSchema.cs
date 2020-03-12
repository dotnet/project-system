// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Defines the structure of identifiers for graph nodes in the dependencies tree.
    /// </summary>
    internal static class DependenciesGraphSchema
    {
        public static readonly GraphSchema Schema = new GraphSchema("Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesSchema");
        public static readonly GraphCategory CategoryDependency = Schema.Categories.AddNewCategory(VSResources.GraphNodeCategoryDependency);

        private const string DependencyIdPropertyId = "Dependency.Id";
        public static readonly GraphProperty DependencyIdProperty = Schema.Properties.AddNewProperty(DependencyIdPropertyId, typeof(string));

        private const string ResolvedPropertyId = "Dependency.Resolved";
        public static readonly GraphProperty ResolvedProperty = Schema.Properties.AddNewProperty(ResolvedPropertyId, typeof(bool));
    }
}
