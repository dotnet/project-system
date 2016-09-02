// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Contains graph node ids and properties for Dependencies nodes
    /// </summary>
    internal class DependenciesGraphSchema
    {
        public static readonly GraphSchema Schema = new GraphSchema("Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesSchema");
        public static readonly GraphCategory CategoryDependency = Schema.Categories.AddNewCategory(VSResources.GraphNodeCategoryDependency);

        private static readonly string ProviderPropertyId = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.ProviderProperyId";
        public static readonly GraphProperty ProviderProperty;
        private static readonly string DependencyPropertyId = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependencyPropertyId";
        public static readonly GraphProperty DependencyNodeProperty;

        static DependenciesGraphSchema()
        {
            ProviderProperty = Schema.Properties.AddNewProperty(ProviderPropertyId, typeof(IProjectDependenciesSubTreeProvider));
            DependencyNodeProperty = Schema.Properties.AddNewProperty(DependencyPropertyId, typeof(IDependencyNode));
        }
    }
}
