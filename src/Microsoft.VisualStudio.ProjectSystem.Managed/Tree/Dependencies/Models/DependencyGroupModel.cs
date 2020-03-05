// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    /// <summary>
    /// Represents a node that groups dependencies from a given provider.
    /// </summary>
    internal class DependencyGroupModel : DependencyModel
    {
        public override string ProviderType { get; }

        public override DependencyIconSet IconSet { get; }

        public DependencyGroupModel(
            string providerType,
            string name,
            DependencyIconSet iconSet,
            ProjectTreeFlags flags)
            : base(
                name,
                originalItemSpec: name,
                flags: flags + ProjectTreeFlags.VirtualFolder + DependencyTreeFlags.DependencyGroup,
                isResolved: true,
                isImplicit: false,
                properties: null)
        {
            ProviderType = providerType;
            IconSet = iconSet;
        }
    }
}
