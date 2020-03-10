// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

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
