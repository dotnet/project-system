// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Changes resolved top level project dependencies to unresolved if:
    ///     - dependent project does not have targets supporting given target framework in current project
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
            ImmutableHashSet<IDependency>.Builder topLevelBuilder)
        {
            IDependency resultDependency = dependency;

            if (resultDependency.TopLevel
                && resultDependency.Resolved                
                && resultDependency.Flags.Contains(DependencyTreeFlags.ProjectNodeFlags)
                && !resultDependency.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
            {
                var snapshot = GetSnapshot(projectPath, resultDependency, out string dependencyProjectPath);
                if (snapshot == null || snapshot.HasUnresolvedDependency)
                {
                    var unresolvedFlags = dependency.Flags.Union(DependencyTreeFlags.UnresolvedFlags)
                                                          .Except(DependencyTreeFlags.ResolvedFlags);
                    resultDependency = resultDependency.SetProperties(resolved: false, flags: unresolvedFlags);
                }
            }

            return resultDependency;
        }

        private ITargetedDependenciesSnapshot GetSnapshot(string projectPath, IDependency dependency, out string dependencyProjectPath)
        {
            dependencyProjectPath = dependency.GetActualPath(projectPath);

            var snapshotProvider = AggregateSnapshotProvider.GetSnapshotProvider(dependencyProjectPath);
            if (snapshotProvider == null)
            {
                return null;
            }

            var snapshot = snapshotProvider.CurrentSnapshot;
            if (snapshot == null)
            {
                return null;
            }

            var targetFramework = TargetFrameworkProvider.GetNearestFramework(
                                    dependency.Snapshot.TargetFramework, snapshot.Targets.Keys);
            if (targetFramework == null)
            {
                return null;
            }

            return snapshot.Targets[targetFramework];
        }
    }
}
