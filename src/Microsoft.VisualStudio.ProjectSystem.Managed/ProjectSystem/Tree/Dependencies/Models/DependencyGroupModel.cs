// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
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
                caption: name,
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
