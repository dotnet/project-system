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
            ITargetedDependenciesSnapshot targetedSnapshot = GetSnapshot(projectPath, dependency, out string dependencyProjectPath);

            return targetedSnapshot?.TopLevelDependencies.Count > 0;
        }

        public override void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            ITargetedDependenciesSnapshot targetedSnapshot)
        {
            // store refreshed dependency
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

            ITargetedDependenciesSnapshot otherProjectTargetedSnapshot = GetSnapshot(projectPath, dependency, out string dependencyProjectPath);
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
                    dependencyProjectPath,
                    dependencyGraphNode,
                    childDependency.ToViewModel(otherProjectTargetedSnapshot));
            }
        }

        private ITargetedDependenciesSnapshot GetSnapshot(string projectPath, IDependency dependency, out string dependencyProjectPath)
        {
            dependencyProjectPath = dependency.FullPath;

            IDependenciesSnapshotProvider snapshotProvider = AggregateSnapshotProvider.GetSnapshotProvider(dependencyProjectPath);
            if (snapshotProvider == null)
            {
                return null;
            }

            IDependenciesSnapshot snapshot = snapshotProvider.CurrentSnapshot;
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

        public override bool ShouldTrackChanges(string projectPath, string updatedProjectPath, IDependency dependency)
        {
            string dependencyProjectPath = dependency.FullPath;
            return !string.IsNullOrEmpty(dependencyProjectPath)
                    && dependencyProjectPath.Equals(updatedProjectPath, StringComparison.OrdinalIgnoreCase);
        }

        public override bool TrackChanges(
                    IGraphContext graphContext,
                    string projectPath,
                    IDependency updatedDependency,
                    GraphNode dependencyGraphNode,
                    ITargetedDependenciesSnapshot targetedSnapshot)
        {
            if (!AnyChanges(projectPath,
                            targetedSnapshot,
                            updatedDependency,
                            dependencyGraphNode,
                            out IEnumerable<DependencyNodeInfo> nodesToAdd,
                            out IEnumerable<DependencyNodeInfo> nodesToRemove,
                            out IEnumerable<IDependency> updatedChildren,
                            out string dependencyProjectPath))
            {
                return false;
            }

            foreach (DependencyNodeInfo nodeToRemove in nodesToRemove)
            {
                Builder.RemoveGraphNode(graphContext, dependencyProjectPath, nodeToRemove.Id, dependencyGraphNode);
            }

            foreach (DependencyNodeInfo nodeToAdd in nodesToAdd)
            {
                IDependency dependency = updatedChildren.FirstOrDefault(x => x.Id.Equals(nodeToAdd.Id));
                if (dependency == null || !dependency.Visible)
                {
                    continue;
                }

                Builder.AddGraphNode(
                    graphContext,
                    dependencyProjectPath,
                    dependencyGraphNode,
                    dependency.ToViewModel(targetedSnapshot));
            }

            // Update the node info saved on the 'inputNode'
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, updatedDependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, updatedDependency.Resolved);

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

            ITargetFramework nearestTargetFramework = TargetFrameworkProvider.GetNearestFramework(
                topLevelDependency.TargetFramework,
                contextResults.Select(x => x.TargetFramework));
            if (nearestTargetFramework == null)
            {
                return true;
            }

            IEnumerable<IDependency> targetedResultsFromContext =
                contextResults.Where(x => nearestTargetFramework.Equals(x.TargetFramework));
            if (targetedResultsFromContext != null)
            {
                topLevelDependencyMatches.AddRange(targetedResultsFromContext);
            }

            return true;
        }

        private bool AnyChanges(
            string projectPath,
            ITargetedDependenciesSnapshot targetedSnapshot,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            out IEnumerable<DependencyNodeInfo> nodesToAdd,
            out IEnumerable<DependencyNodeInfo> nodesToRemove,
            out IEnumerable<IDependency> updatedChildren,
            out string dependencyProjectPath)
        {
            ITargetedDependenciesSnapshot snapshot = GetSnapshot(projectPath, updatedDependency, out dependencyProjectPath);
            if (snapshot == null)
            {
                nodesToAdd = Enumerable.Empty<DependencyNodeInfo>();
                nodesToRemove = Enumerable.Empty<DependencyNodeInfo>();
                updatedChildren = Enumerable.Empty<IDependency>();
                dependencyProjectPath = string.Empty;
                return false;
            }

            updatedChildren = snapshot.TopLevelDependencies;
            IEnumerable<DependencyNodeInfo> existingChildren = GetExistingChildren(dependencyGraphNode);
            IEnumerable<DependencyNodeInfo> updatedChildrenInfo = updatedChildren.Select(x => DependencyNodeInfo.FromDependency(x));

            return AnyChanges(existingChildren, updatedChildrenInfo, out nodesToAdd, out nodesToRemove);
        }
    }
}
