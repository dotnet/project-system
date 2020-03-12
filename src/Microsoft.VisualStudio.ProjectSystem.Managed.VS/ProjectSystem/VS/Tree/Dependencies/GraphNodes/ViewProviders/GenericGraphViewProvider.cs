// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    [Export(typeof(IDependenciesGraphViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class GenericGraphViewProvider : GraphViewProviderBase
    {
        public const int Order = 0;

        [ImportingConstructor]
        public GenericGraphViewProvider(IDependenciesGraphBuilder builder)
            : base(builder)
        {
        }

        public override void BuildGraph(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode dependencyGraphNode,
            TargetedDependenciesSnapshot targetedSnapshot)
        {
            // store refreshed dependency info
            dependencyGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
            dependencyGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

            ImmutableArray<IDependency> children = targetedSnapshot.GetDependencyChildren(dependency);

            if (children.IsEmpty)
            {
                return;
            }

            foreach (IDependency childDependency in children)
            {
                if (!childDependency.Visible)
                {
                    continue;
                }

                Builder.AddGraphNode(
                    graphContext,
                    projectPath,
                    dependencyGraphNode,
                    childDependency.ToViewModel(targetedSnapshot));
            }
        }
    }
}
