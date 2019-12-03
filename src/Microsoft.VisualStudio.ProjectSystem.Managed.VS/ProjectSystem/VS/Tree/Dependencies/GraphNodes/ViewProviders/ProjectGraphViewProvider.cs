// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    /// <summary>
    /// Provides the graph of project reference dependencies.
    /// Allows drilling into the transitive dependencies of a given <c>&lt;ProjectReference&gt;</c>.
    /// </summary>
    [Export(typeof(IDependenciesGraphViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class ProjectGraphViewProvider : GraphViewProviderBase
    {
        public const int Order = 110;

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public ProjectGraphViewProvider(
            IDependenciesGraphBuilder builder,
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
            ITargetFrameworkProvider targetFrameworkProvider)
            : base(builder)
        {
            _aggregateSnapshotProvider = aggregateSnapshotProvider;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public override bool SupportsDependency(IDependency dependency)
        {
            // Only supports project reference dependencies
            return dependency.IsProject();
        }

        public override bool HasChildren(IDependency dependency)
        {
            TargetedDependenciesSnapshot? targetedSnapshot = _aggregateSnapshotProvider.GetSnapshot(dependency);

            return targetedSnapshot?.TopLevelDependencies.Length != 0;
        }

        public override void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot)
        {
            // store refreshed dependency
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

            TargetedDependenciesSnapshot? otherProjectTargetedSnapshot = _aggregateSnapshotProvider.GetSnapshot(dependency);

            if (otherProjectTargetedSnapshot == null)
            {
                return;
            }

            foreach (IDependency childDependency in otherProjectTargetedSnapshot.TopLevelDependencies)
            {
                if (!childDependency.Visible)
                {
                    continue;
                }

                Builder.AddGraphNode(
                    graphContext,
                    dependency.FullPath,
                    dependencyGraphNode,
                    childDependency.ToViewModel(otherProjectTargetedSnapshot));
            }
        }

        /// <summary>
        /// Returns true if the updated dependency's path matches the updated snapshot's project path,
        /// meaning the project dependency has changed and we want to try and update.
        /// </summary>
        public override bool ShouldApplyChanges(string nodeProjectPath, string updatedSnapshotProjectPath, IDependency updatedDependency)
        {
            string dependencyProjectPath = updatedDependency.FullPath;
            return !string.IsNullOrEmpty(dependencyProjectPath)
                    && dependencyProjectPath.Equals(updatedSnapshotProjectPath, StringComparisons.Paths);
        }

        public override bool ApplyChanges(
            IGraphContext graphContext,
            string nodeProjectPath,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot)
        {
            TargetedDependenciesSnapshot? referencedProjectSnapshot = _aggregateSnapshotProvider.GetSnapshot(updatedDependency);

            if (referencedProjectSnapshot == null)
            {
                return false;
            }

            return ApplyChangesInternal(
                graphContext,
                updatedDependency,
                dependencyGraphNode,
                // Project references list all top level dependencies as direct children
                updatedChildren: referencedProjectSnapshot.TopLevelDependencies,
                // Pass the path of the referenced project
                nodeProjectPath: updatedDependency.FullPath,
                targetedSnapshot: referencedProjectSnapshot);
        }

        public override bool MatchSearchResults(
            IDependency topLevelDependency,
            Dictionary<string, HashSet<IDependency>> searchResultsPerContext,
            [NotNullWhen(returnValue: true)] out HashSet<IDependency>? topLevelDependencyMatches)
        {
            if (!topLevelDependency.Flags.Contains(DependencyTreeFlags.ProjectDependency))
            {
                topLevelDependencyMatches = null;
                return false;
            }

            topLevelDependencyMatches = new HashSet<IDependency>(DependencyIdComparer.Instance);

            if (!topLevelDependency.Visible)
            {
                return true;
            }

            string projectFullPath = topLevelDependency.FullPath;

            if (!searchResultsPerContext.TryGetValue(projectFullPath, out HashSet<IDependency> contextResults)
                || contextResults.Count == 0)
            {
                return true;
            }

            ITargetFramework? nearestTargetFramework = _targetFrameworkProvider.GetNearestFramework(
                topLevelDependency.TargetFramework,
                contextResults.Select(x => x.TargetFramework));

            if (nearestTargetFramework == null)
            {
                return true;
            }

            IEnumerable<IDependency> targetedResultsFromContext =
                contextResults.Where(x => nearestTargetFramework.Equals(x.TargetFramework));

            topLevelDependencyMatches.AddRange(targetedResultsFromContext);

            return true;
        }
    }
}
