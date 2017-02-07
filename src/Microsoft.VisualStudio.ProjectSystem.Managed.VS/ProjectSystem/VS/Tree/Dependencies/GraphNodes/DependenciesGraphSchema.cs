// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Contains graph node ids and properties for Dependencies nodes
    /// </summary>
    internal class DependenciesGraphSchema
    {
        public static readonly GraphSchema Schema = new GraphSchema("Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesSchema");
        public static readonly GraphCategory CategoryDependency = Schema.Categories.AddNewCategory(VSResources.GraphNodeCategoryDependency);

        private static readonly string DependencyPropertyId = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependencyPropertyId";
        public static readonly GraphProperty DependencyProperty;

        private static readonly string IsFrameworkAssemblyFolderPropertyId = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.IsFrameworkAssembly";
        public static readonly GraphProperty IsFrameworkAssemblyFolderProperty;

        static DependenciesGraphSchema()
        {
            IsFrameworkAssemblyFolderProperty = Schema.Properties.AddNewProperty(IsFrameworkAssemblyFolderPropertyId, typeof(bool));
            DependencyProperty = Schema.Properties.AddNewProperty(DependencyPropertyId, typeof(IDependency));
        }
    }
}
