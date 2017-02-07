// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    [Export(typeof(IDependenciesGraphViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class ProjectGraphViewProvider : GraphViewProviderBase
    {
        public const int Order = 110;

        [ImportingConstructor]
        public ProjectGraphViewProvider(IDependenciesGraphBuilder builder,
                                        IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                                        ITargetFrameworkProvider targetFrameworkProvider)
            : base(builder)
        {
            AggregateSnapshotProvider = aggregateSnapshotProvider;
            TargetFrameworkProvider = targetFrameworkProvider;
        }

        private IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }
        private ITargetFrameworkProvider TargetFrameworkProvider { get; }

        public override bool SupportsDependency(IDependency dependency)
        {
            return dependency.IsProject();
        }

        public override bool HasChildren(string projectPath, IDependency dependency)
        {
            var targetedSnapshot = GetSnapshot(projectPath, dependency, out string dependencyProjectPath);

            return targetedSnapshot?.TopLevelDependencies.Count > 0;
        }

        public override void BuildGraph(IGraphContext graphContext,
                                          string projectPath,
                                          IDependency dependency,
                                          GraphNode dependencyGraphNode)
        {
            // store refreshed dependency
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyProperty, dependency);
            var targetedSnapshot = GetSnapshot(projectPath, dependency, out string dependencyProjectPath);
            if (targetedSnapshot == null)
            {
                return;
            }

            foreach (var childDependency in targetedSnapshot.TopLevelDependencies)
            {
                if (!childDependency.Visible)
                {
                    continue;
                }

                Builder.AddGraphNode(graphContext, dependencyProjectPath, dependencyGraphNode, childDependency);
            }
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

        public override bool ShouldTrackChanges(string projectPath, string updatedProjectPath, IDependency dependency)
        {
            var dependencyProjectPath = dependency.GetActualPath(projectPath);
            return !string.IsNullOrEmpty(dependencyProjectPath)
                    && dependencyProjectPath.Equals(updatedProjectPath, StringComparison.OrdinalIgnoreCase);
        }

        public override bool TrackChanges(
                    IGraphContext graphContext,
                    string projectPath,
                    IDependency existingDependency,
                    IDependency updatedDependency,
                    GraphNode dependencyGraphNode)
        {
            if (!AnyChanges(projectPath,
                            existingDependency,
                            updatedDependency,
                            dependencyGraphNode,
                            out IEnumerable<IDependency> nodesToAdd,
                            out IEnumerable<IDependency> nodesToRemove,
                            out string dependencyProjectPath))
            {
                return false;
            }

            foreach (var nodeToRemove in nodesToRemove)
            {
                Builder.RemoveGraphNode(graphContext, dependencyProjectPath, nodeToRemove, dependencyGraphNode);
            }

            foreach (var nodeToAdd in nodesToAdd)
            {
                Builder.AddGraphNode(graphContext, dependencyProjectPath, dependencyGraphNode, nodeToAdd);
            }

            // Update the node info saved on the 'inputNode'
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyProperty, updatedDependency);

            return true;
        }

        public override bool MatchSearchResults(
            string projectPath, 
            IDependency topLevelDependency,
            Dictionary<string, HashSet<IDependency>> searchResultsPerContext,
            out HashSet<IDependency> topLevelDependencyMatches)
        {
            topLevelDependencyMatches = new HashSet<IDependency>();

            if (!topLevelDependency.Flags.Contains(DependencyTreeFlags.ProjectNodeFlags))
            {
                return false;
            }

            if (!topLevelDependency.Resolved || !topLevelDependency.Visible)
            {
                return true;
            }

            var projectFullPath = topLevelDependency.GetActualPath(projectPath);
            if (!searchResultsPerContext.TryGetValue(projectFullPath, out HashSet<IDependency> contextResults)
                || contextResults.Count == 0)
            {
                return true;
            }

            var nearestTargetFramework = TargetFrameworkProvider.GetNearestFramework(
                topLevelDependency.Snapshot.TargetFramework,
                contextResults.Select(x => x.Snapshot.TargetFramework));
            if (nearestTargetFramework == null)
            {
                return true;
            }

            var targetedResultsFromContext =
                contextResults.Where(x => nearestTargetFramework.Equals(x.Snapshot.TargetFramework));
            if (targetedResultsFromContext != null)
            {
                topLevelDependencyMatches.AddRange(targetedResultsFromContext);
            }

            return true;
        }

        protected override bool AnyChanges(
            string projectPath,
            IDependency existingDependency,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            out IEnumerable<IDependency> nodesToAdd,
            out IEnumerable<IDependency> nodesToRemove,
            out string dependencyProjectPath)
        {
            var snapshot = GetSnapshot(projectPath, updatedDependency, out dependencyProjectPath);
            if (snapshot == null)
            {
                nodesToAdd = Enumerable.Empty<IDependency>();
                nodesToRemove = Enumerable.Empty<IDependency>();
                dependencyProjectPath = string.Empty;
                return false;
            }

            var existingChildren = new List<IDependency>();
            var descendants = dependencyGraphNode.FindDescendants();
            foreach (var node in descendants)
            {
                var dependency = node.GetValue<IDependency>(DependenciesGraphSchema.DependencyProperty);
                if (dependency == null)
                {
                    continue;
                }

                existingChildren.Add(dependency);
            }

            return AnyChanges(existingChildren, snapshot.TopLevelDependencies, out nodesToAdd, out nodesToRemove);
        }
    }
}
