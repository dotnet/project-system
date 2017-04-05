// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override void BuildGraph(IGraphContext graphContext,
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
    }
}
