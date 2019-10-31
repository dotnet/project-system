// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Marks project references as unresolved where the referenced project contains a visible unresolved dependency.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class UnresolvedProjectReferenceSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 120;

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;

        [ImportingConstructor]
        public UnresolvedProjectReferenceSnapshotFilter(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
        {
            _aggregateSnapshotProvider = aggregateSnapshotProvider;
        }

        public override void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            if (dependency.TopLevel
                && dependency.Resolved
                && dependency.Flags.Contains(DependencyTreeFlags.ProjectDependency)
                && !dependency.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
            {
                TargetedDependenciesSnapshot? snapshot = _aggregateSnapshotProvider.GetSnapshot(dependency);

                if (snapshot != null && snapshot.HasReachableVisibleUnresolvedDependency)
                {
                    context.Accept(dependency.ToUnresolved(ProjectReference.SchemaName));
                    return;
                }
            }

            context.Accept(dependency);
        }
    }
}
