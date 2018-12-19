// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
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
    internal class ProjectGraphViewProvider : GraphViewProviderBase
    {
        public const int Order = 110;

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public ProjectGraphViewProvider(IDependenciesGraphBuilder builder,
                                        IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                                        ITargetFrameworkProvider targetFrameworkProvider)
            : base(builder)
        {
            _aggregateSnapshotProvider = aggregateSnapshotProvider;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public override bool SupportsDependency(IDependency dependency)
        {
            return dependency.IsProject();
        }

        public override bool HasChildren(string projectPath, IDependency dependency)
        {
            ITargetedDependenciesSnapshot targetedSnapshot = GetSnapshot(dependency);

            return targetedSnapshot?.TopLevelDependencies.Length != 0;
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

            ITargetedDependenciesSnapshot otherProjectTargetedSnapshot = GetSnapshot(dependency);
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

        private ITargetedDependenciesSnapshot GetSnapshot(IDependency dependency)
        {
            IDependenciesSnapshot snapshot =
                _aggregateSnapshotProvider.GetSnapshotProvider(dependency.FullPath)?.CurrentSnapshot;

            if (snapshot == null)
            {
                return null;
            }

            ITargetFramework targetFramework = _targetFrameworkProvider.GetNearestFramework(
                                    dependency.TargetFramework, snapshot.Targets.Keys);

            if (targetFramework == null)
            {
                return null;
            }

            return snapshot.Targets[targetFramework];
        }

        /// <summary>
        /// Returns true if the updated dependency's path matches the updated snapshot's project path,
        /// meaning the project dependency has changed and we want to try and update.
        /// </summary>
        /// <inheritdoc />
        public override bool ShouldApplyChanges(string projectPath, string updatedProjectPath, IDependency dependency)
        {
            string dependencyProjectPath = dependency.FullPath;
            return !string.IsNullOrEmpty(dependencyProjectPath)
                    && dependencyProjectPath.Equals(updatedProjectPath, StringComparison.OrdinalIgnoreCase);
        }

        public override bool ApplyChanges(
                    IGraphContext graphContext,
                    string projectPath,
                    IDependency updatedDependency,
                    GraphNode dependencyGraphNode,
                    ITargetedDependenciesSnapshot targetedSnapshot)
        {
            if (!AnyChanges(updatedDependency,
                            dependencyGraphNode,
                            out IReadOnlyList<DependencyNodeInfo> nodesToAdd,
                            out IReadOnlyList<DependencyNodeInfo> nodesToRemove,
                            out ImmutableArray<IDependency> updatedChildren))
            {
                return false;
            }

            string dependencyProjectPath = updatedDependency.FullPath;

            bool anyChanges = false;

            foreach (DependencyNodeInfo nodeToRemove in nodesToRemove)
            {
                anyChanges = true;
                Builder.RemoveGraphNode(graphContext, dependencyProjectPath, nodeToRemove.Id, dependencyGraphNode);
            }

            foreach (DependencyNodeInfo nodeToAdd in nodesToAdd)
            {
                IDependency dependency = updatedChildren.FirstOrDefault((x, id) => x.Id.Equals(id), nodeToAdd.Id);
                if (dependency == null || !dependency.Visible)
                {
                    continue;
                }

                anyChanges = true;
                Builder.AddGraphNode(
                    graphContext,
                    dependencyProjectPath,
                    dependencyGraphNode,
                    dependency.ToViewModel(targetedSnapshot));
            }

            // Update the node info saved on the 'inputNode'
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, updatedDependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, updatedDependency.Resolved);

            return anyChanges;
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

            ITargetFramework nearestTargetFramework = _targetFrameworkProvider.GetNearestFramework(
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

        private bool AnyChanges(
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            out IReadOnlyList<DependencyNodeInfo> nodesToAdd,
            out IReadOnlyList<DependencyNodeInfo> nodesToRemove,
            out ImmutableArray<IDependency> updatedChildren)
        {
            ITargetedDependenciesSnapshot snapshot = GetSnapshot(updatedDependency);

            if (snapshot == null)
            {
                nodesToAdd = default;
                nodesToRemove = default;
                updatedChildren = default;
                return false;
            }

            updatedChildren = snapshot.TopLevelDependencies;
            IReadOnlyList<DependencyNodeInfo> existingChildren = GetExistingChildren(dependencyGraphNode);
            IReadOnlyList<DependencyNodeInfo> updatedChildrenInfo = updatedChildren.Select(x => DependencyNodeInfo.FromDependency(x)).ToList();

            return AnyChanges(existingChildren, updatedChildrenInfo, out nodesToAdd, out nodesToRemove);
        }
    }
}
