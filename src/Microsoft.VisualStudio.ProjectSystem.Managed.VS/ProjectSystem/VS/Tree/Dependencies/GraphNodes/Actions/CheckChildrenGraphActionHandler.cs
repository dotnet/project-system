// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
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

                IDependency dependency = GetDependency(graphContext, inputGraphNode, out IDependenciesSnapshot snapshot);
                if (dependency == null || snapshot == null)
                {
                    continue;
                }

                System.Lazy<ViewProviders.IDependenciesGraphViewProvider, IOrderPrecedenceMetadataView> viewProvider = ViewProviders.FirstOrDefault(x => x.Value.SupportsDependency(dependency));
                if (viewProvider == null)
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
                    inputGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

                    if (viewProvider.Value.HasChildren(projectPath, dependency))
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
