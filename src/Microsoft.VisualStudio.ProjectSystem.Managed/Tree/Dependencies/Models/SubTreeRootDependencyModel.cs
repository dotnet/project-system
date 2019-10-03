// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SubTreeRootDependencyModel : DependencyModel
    {
        public override string ProviderType { get; }

        public override DependencyIconSet IconSet { get; }

        public SubTreeRootDependencyModel(
            string providerType,
            string name,
            DependencyIconSet iconSet)
            : base(
                name,
                originalItemSpec: name,
                flags: DependencyTreeFlags.SubTreeRootNode,
                isResolved: true,
                isImplicit: false,
                properties: null)
        {
            ProviderType = providerType;
            IconSet = iconSet;
        }
    }
}
