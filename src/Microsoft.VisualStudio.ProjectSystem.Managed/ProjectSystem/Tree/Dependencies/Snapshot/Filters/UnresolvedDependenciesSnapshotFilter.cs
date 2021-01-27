// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
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
            IDependency dependency,
            AddDependencyContext context)
        {
            // TODO should this verify that the existing one is actually resolved?
            if (!dependency.Resolved && context.Contains(dependency.GetDependencyId()))
            {
                context.Reject();
                return;
            }

            context.Accept(dependency);
        }
    }
}
