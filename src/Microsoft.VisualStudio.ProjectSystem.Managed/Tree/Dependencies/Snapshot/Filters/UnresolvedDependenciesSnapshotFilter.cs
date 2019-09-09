// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Prohibits the unresolved dependency rule (evaluation) from overriding the corresponding 
    /// resolved rule (design-time build) in the snapshot. 
    /// </summary>
    /// <remarks>
    /// Once resolved, a dependency cannot revert to unresolved state. It will only appear as
    /// unresolved again if it is first removed.
    /// </remarks>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class UnresolvedDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 100;

        public override void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            // TODO should this verify that the existing one is actually resolved?
            if (!dependency.Resolved && context.Contains(dependency.Id))
            {
                context.Reject();
                return;
            }

            context.Accept(dependency);
        }
    }
}
