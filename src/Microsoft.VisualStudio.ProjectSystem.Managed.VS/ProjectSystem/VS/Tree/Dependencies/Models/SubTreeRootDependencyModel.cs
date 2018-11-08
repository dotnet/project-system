// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SubTreeRootDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(
            add: DependencyTreeFlags.DependencyFlags +
                 DependencyTreeFlags.SubTreeRootNodeFlags,
            remove: DependencyTreeFlags.SupportsRuleProperties +
                    DependencyTreeFlags.SupportsRemove);

        public override string ProviderType { get; }

        public override DependencyIconSet IconSet { get; }

        public SubTreeRootDependencyModel(
            string providerType,
            string name,
            DependencyIconSet iconSet,
            ProjectTreeFlags flags)
            : base(
                name,
                originalItemSpec: name,
                flags: flags + s_flagCache.Get(isResolved: true, isImplicit: false),
                isResolved: true,
                isImplicit: false,
                properties: null)
        {
            ProviderType = providerType;
            IconSet = iconSet;
        }
    }
}
