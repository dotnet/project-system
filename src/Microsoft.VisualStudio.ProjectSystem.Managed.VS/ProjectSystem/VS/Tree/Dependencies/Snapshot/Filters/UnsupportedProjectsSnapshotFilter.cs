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
    internal class UnsupportedProjectsSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 120;

        [ImportingConstructor]
        public UnsupportedProjectsSnapshotFilter(
                IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                ITargetFrameworkProvider targetFrameworkProvider)
        {
            AggregateSnapshotProvider = aggregateSnapshotProvider;
            TargetFrameworkProvider = targetFrameworkProvider;
        }

        private IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }
        private ITargetFrameworkProvider TargetFrameworkProvider { get; }

        public override IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
            IImmutableSet<string> projectItemSpecs,
            out bool filterAnyChanges)
        {
            filterAnyChanges = false;

            if (dependency.TopLevel
                && dependency.Resolved
                && dependency.Flags.Contains(DependencyTreeFlags.ProjectNodeFlags)
                && !dependency.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
            {
                ITargetedDependenciesSnapshot snapshot = GetSnapshot(dependency);
                if (snapshot != null && snapshot.HasUnresolvedDependency)
                {
                    filterAnyChanges = true;
                    return dependency.ToUnresolved(ProjectReference.SchemaName);
                }
            }

            return dependency;
        }

        private ITargetedDependenciesSnapshot GetSnapshot(IDependency dependency)
        {
            IDependenciesSnapshot snapshot = 
                AggregateSnapshotProvider.GetSnapshotProvider(dependency.FullPath)?.CurrentSnapshot;

            if (snapshot == null)
            {
                return null;
            }

            ITargetFramework targetFramework = TargetFrameworkProvider.GetNearestFramework(
                                    dependency.TargetFramework, snapshot.Targets.Keys);
            if (targetFramework == null)
            {
                return null;
            }

            return snapshot.Targets[targetFramework];
        }
    }
}
