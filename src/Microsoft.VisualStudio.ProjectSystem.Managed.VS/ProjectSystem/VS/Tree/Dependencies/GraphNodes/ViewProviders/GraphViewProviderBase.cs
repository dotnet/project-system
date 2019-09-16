// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

        public virtual bool HasChildren(IDependency dependency)
        {
            return dependency.DependencyIDs.Length != 0;
        }

        public abstract void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot);

        public virtual bool ShouldApplyChanges(string nodeProjectPath, string updatedSnapshotProjectPath, IDependency updatedDependency)
        {
            return nodeProjectPath.Equals(updatedSnapshotProjectPath, StringComparisons.Paths);
        }

        public virtual bool ApplyChanges(
            IGraphContext graphContext,
            string nodeProjectPath,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot)
        {
            return ApplyChangesInternal(
                graphContext,
                updatedDependency,
                dependencyGraphNode,
                updatedChildren: targetedSnapshot.GetDependencyChildren(updatedDependency),
                nodeProjectPath: nodeProjectPath,
                targetedSnapshot);
        }

        protected bool ApplyChangesInternal(
            IGraphContext graphContext,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            ImmutableArray<IDependency> updatedChildren,
            string nodeProjectPath,
            TargetedDependenciesSnapshot targetedSnapshot)
        {
            IEnumerable<string> existingChildModelIds = GetExistingChildren();
            IEnumerable<string> updatedChildModelIds = updatedChildren.Select(dependency => dependency.Id);

            var diff = new SetDiff<string>(existingChildModelIds, updatedChildModelIds);

            bool anyChanges = false;

            foreach (string childModelIdToRemove in diff.Removed)
            {
                anyChanges = true;
                Builder.RemoveGraphNode(graphContext, nodeProjectPath, childModelIdToRemove, dependencyGraphNode);
            }

            foreach (string childModeIdToAdd in diff.Added)
            {
                if (!targetedSnapshot.DependenciesWorld.TryGetValue(childModeIdToAdd, out IDependency dependency)
                    || !dependency.Visible)
                {
                    continue;
                }

                anyChanges = true;
                Builder.AddGraphNode(
                    graphContext,
                    nodeProjectPath,
                    dependencyGraphNode,
                    dependency.ToViewModel(targetedSnapshot));
            }

            // Update the node info saved on the 'inputNode'
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, updatedDependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, updatedDependency.Resolved);

            return anyChanges;

            IEnumerable<string> GetExistingChildren()
            {
                foreach (GraphNode childNode in dependencyGraphNode.FindDescendants())
                {
                    string id = childNode.GetValue<string>(DependenciesGraphSchema.DependencyIdProperty);

                    if (!string.IsNullOrEmpty(id))
                    {
                        yield return id;
                    }
                }
            }
        }

        public virtual bool MatchSearchResults(
            IDependency topLevelDependency,
            Dictionary<string, HashSet<IDependency>> searchResultsPerContext,
            [NotNullWhen(returnValue: true)] out HashSet<IDependency>? topLevelDependencyMatches)
        {
            topLevelDependencyMatches = null;
            return false;
        }
    }
}
