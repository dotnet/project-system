// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    internal abstract class GraphViewProviderBase : IDependenciesGraphViewProvider
    {
        public GraphViewProviderBase(IDependenciesGraphBuilder builder)
        {
            Builder = builder;
        }

        protected IDependenciesGraphBuilder Builder { get; }

        public virtual bool SupportsDependency(IDependency dependency)
        {
            // we support any dependency type
            return true;
        }

        public virtual bool HasChildren(string projectPath, IDependency dependency)
        {
            return dependency.DependencyIDs.Count > 0;
        }

        public virtual void BuildGraph(IGraphContext graphContext, 
                                          string projectPath, 
                                          IDependency dependency, 
                                          GraphNode dependencyGraphNode)
        {
            // store refreshed dependency
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyProperty, dependency);
            foreach (var childDependency in dependency.Dependencies)
            {
                if (!childDependency.Visible)
                {
                    continue;
                }

                Builder.AddGraphNode(graphContext, projectPath, dependencyGraphNode, childDependency);
            }
        }

        public virtual bool ShouldTrackChanges(string projectPath, string updatedProjectPath, IDependency dependency)
        {
            return projectPath.Equals(updatedProjectPath, StringComparison.OrdinalIgnoreCase);
        }

        public virtual bool TrackChanges(
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
                Builder.RemoveGraphNode(graphContext, projectPath, nodeToRemove, dependencyGraphNode);
            }

            foreach (var nodeToAdd in nodesToAdd)
            {
                Builder.AddGraphNode(graphContext, projectPath, dependencyGraphNode, nodeToAdd);
            }

            // Update the node info saved on the 'inputNode'
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyProperty, updatedDependency);

            return true;
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

        protected virtual bool AnyChanges(
            string projectPath,
            IDependency existingDependency,
            IDependency updatedDependency,
            GraphNode dependencyGraphNode,
            out IEnumerable<IDependency> nodesToAdd,
            out IEnumerable<IDependency> nodesToRemove,
            out string dependencyProjectPath)
        {
            dependencyProjectPath = projectPath;

            var existingChildren = existingDependency.Dependencies;
            var updatedChildren = updatedDependency.Dependencies;

            return AnyChanges(existingChildren, updatedChildren, out nodesToAdd, out nodesToRemove);
        }

        protected bool AnyChanges(
            IEnumerable<IDependency> existingChildren,
            IEnumerable<IDependency> updatedChildren,
            out IEnumerable<IDependency> nodesToAdd,
            out IEnumerable<IDependency> nodesToRemove)
        {

            var comparer = new DependencyResolvedStateComparer();
            nodesToRemove = existingChildren.Except(updatedChildren, comparer).ToList();
            nodesToAdd = updatedChildren.Except(existingChildren, comparer).ToList();
            return nodesToAdd.Any() || nodesToRemove.Any();
        }
    }
}
