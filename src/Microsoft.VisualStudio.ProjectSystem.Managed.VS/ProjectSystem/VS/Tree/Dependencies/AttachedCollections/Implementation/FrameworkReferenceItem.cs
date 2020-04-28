// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    internal sealed class FrameworkReferenceItem : RelatableItemBase
    {
        public FrameworkReferenceIdentity Framework { get; }

        public FrameworkReferenceItem(FrameworkReferenceIdentity framework)
            : base(framework.Name)
        {
            Framework = framework;
        }

        public override object Identity => (Framework.Path, Framework.Profile);
        public override int Priority => 0;
        public override ImageMoniker IconMoniker => ManagedImageMonikers.FrameworkPrivate;

        protected override bool TryGetProjectNode(IProjectTree targetRootNode, IRelatableItem item, [NotNullWhen(true)] out IProjectTree? projectTree)
        {
            return base.TryGetProjectNode(targetRootNode, item, out projectTree);
        }
    }
}
