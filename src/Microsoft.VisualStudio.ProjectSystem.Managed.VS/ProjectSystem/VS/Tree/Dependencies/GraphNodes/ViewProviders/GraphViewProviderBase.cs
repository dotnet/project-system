// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    internal abstract class GraphViewProviderBase : IDependenciesGraphViewProvider
    {
        protected GraphViewProviderBase(IDependenciesGraphBuilder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            Builder = builder;
        }

        protected IDependenciesGraphBuilder Builder { get; }

        public virtual bool SupportsDependency(IDependency dependency)
        {
            // Supports all dependencies
            return true;
        }

        public virtual bool HasChildren(string projectPath, IDependency dependency)
        {
            return dependency.DependencyIDs.Count != 0;
        }

        public abstract void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            ITargetedDependenciesSnapshot targetedSnapshot);

        public virtual bool ShouldApplyChanges(string nodeProjectPath, string updatedSnapshotProjectPath, IDependency updatedDependency)
        {
            return nodeProjectPath.Equals(updatedSnapshotProjectPath, StringComparisons.Paths);
        }

        public virtual bool ApplyChanges(
            IGraphContext graphContext,
            string projectPath,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            ITargetedDependenciesSnapshot targetedSnapshot)
        {
            IReadOnlyList<DependencyNodeInfo> existingChildrenInfo = GetExistingChildren(dependencyGraphNode);
            ImmutableArray<IDependency> updatedChildren = targetedSnapshot.GetDependencyChildren(updatedDependency);
            IReadOnlyList<DependencyNodeInfo> updatedChildrenInfo = updatedChildren.Select(DependencyNodeInfo.FromDependency).ToList();

            if (!AnyChanges(
                existingChildrenInfo,
                updatedChildrenInfo,
                out IReadOnlyList<DependencyNodeInfo> nodesToAdd,
                out IReadOnlyList<DependencyNodeInfo> nodesToRemove))
            {
                return false;
            }

            bool anyChanges = false;

            foreach (DependencyNodeInfo nodeToRemove in nodesToRemove)
            {
                anyChanges = true;
                Builder.RemoveGraphNode(graphContext, projectPath, nodeToRemove.Id, dependencyGraphNode);
            }

            foreach (DependencyNodeInfo nodeToAdd in nodesToAdd)
            {
                if (!targetedSnapshot.DependenciesWorld.TryGetValue(nodeToAdd.Id, out IDependency dependency)
                    || dependency == null
                    || !dependency.Visible)
                {
                    continue;
                }

                anyChanges = true;
                Builder.AddGraphNode(
                    graphContext,
                    projectPath,
                    dependencyGraphNode,
                    dependency.ToViewModel(targetedSnapshot));
            }

            // Update the node info saved on the 'inputNode'
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, updatedDependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, updatedDependency.Resolved);

            return anyChanges;
        }

        public virtual bool MatchSearchResults(
            string projectPath,
            IDependency topLevelDependency,
            Dictionary<string, HashSet<IDependency>> searchResultsPerContext,
            out HashSet<IDependency> topLevelDependencyMatches)
        {
            topLevelDependencyMatches = null;
            return false;
        }

        protected static bool AnyChanges(
            IReadOnlyList<DependencyNodeInfo> existingChildren,
            IReadOnlyList<DependencyNodeInfo> updatedChildren,
            out IReadOnlyList<DependencyNodeInfo> nodesToAdd,
            out IReadOnlyList<DependencyNodeInfo> nodesToRemove)
        {
            nodesToRemove = existingChildren.Except(updatedChildren).ToList();
            nodesToAdd = updatedChildren.Except(existingChildren).ToList();

            return nodesToAdd.Count != 0 || nodesToRemove.Count != 0;
        }

        protected static IReadOnlyList<DependencyNodeInfo> GetExistingChildren(GraphNode inputGraphNode)
        {
            var children = new List<DependencyNodeInfo>();

            foreach (GraphNode childNode in inputGraphNode.FindDescendants())
            {
                string id = childNode.GetValue<string>(DependenciesGraphSchema.DependencyIdProperty);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                children.Add(new DependencyNodeInfo(
                    id, 
                    childNode.Label, 
                    childNode.GetValue<bool>(DependenciesGraphSchema.ResolvedProperty)));
            }

            return children;
        }
    }
}
