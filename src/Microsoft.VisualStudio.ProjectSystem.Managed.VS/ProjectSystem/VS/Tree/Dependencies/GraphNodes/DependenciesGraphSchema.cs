// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private const string IsFrameworkAssemblyFolderPropertyId = "Dependency.IsFrameworkAssembly";
        public static readonly GraphProperty IsFrameworkAssemblyFolderProperty = Schema.Properties.AddNewProperty(IsFrameworkAssemblyFolderPropertyId, typeof(bool));
    }
}
