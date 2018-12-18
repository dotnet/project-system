// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    /// Updates input graph nodes to indicate whether or not they have children.
    /// Actually adding the child nodes to the graph is handled separately by
    /// <see cref="GetChildrenGraphActionHandler"/>.
    /// </summary>
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class CheckChildrenGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 100;

        [ImportingConstructor]
        public CheckChildrenGraphActionHandler(IDependenciesGraphBuilder builder,
                                               IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(builder, aggregateSnapshotProvider)
        {
        }

        public override bool CanHandleRequest(IGraphContext graphContext)
        {
            return graphContext.Direction == GraphContextDirection.Self
                    && graphContext.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren);
        }

        public override bool HandleRequest(IGraphContext graphContext)
        {
            foreach (GraphNode inputGraphNode in graphContext.InputNodes)
            {
                if (graphContext.CancelToken.IsCancellationRequested)
                {
                    return false;
                }

                string projectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                IDependency dependency = GetDependency(inputGraphNode, out IDependenciesSnapshot snapshot);
                if (dependency == null || snapshot == null)
                {
                    continue;
                }

                IDependenciesGraphViewProvider? viewProvider = ViewProviders
                    .FirstOrDefaultValue((x, d) => x.SupportsDependency(d), dependency);

                if (viewProvider == null)
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
                    inputGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

                    if (viewProvider.HasChildren(projectPath, dependency))
                    {
                        inputGraphNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
                    }

                    scope.Complete();
                }
            }

            return false;
        }
    }
}
