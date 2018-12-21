// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Changes resolved top level project dependencies to unresolved if:
    ///     - dependent project has any unresolved dependencies in a snapshot for given target framework
    /// This helps to bubble up error status (yellow icon) for project dependencies.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class UnsupportedProjectsSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 120;

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;

        [ImportingConstructor]
        public UnsupportedProjectsSnapshotFilter(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
        {
            _aggregateSnapshotProvider = aggregateSnapshotProvider;
        }

        public override void BeforeAddOrUpdate(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string> projectItemSpecs,
            IAddDependencyContext context)
        {
            if (dependency.TopLevel
                && dependency.Resolved
                && dependency.Flags.Contains(DependencyTreeFlags.ProjectNodeFlags)
                && !dependency.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
            {
                ITargetedDependenciesSnapshot snapshot = _aggregateSnapshotProvider.GetSnapshot(dependency);

                if (snapshot != null && snapshot.HasUnresolvedDependency)
                {
                    context.Accept(dependency.ToUnresolved(ProjectReference.SchemaName));
                    return;
                }
            }

            context.Accept(dependency);
        }
    }
}
