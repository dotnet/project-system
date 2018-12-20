// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    ///     Handles requests to populate populated children of a given dependencies graph node.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Determining whether a node has children or not is performed separately by
    ///     <see cref="CheckChildrenGraphActionHandler"/>.
    /// </para>
    /// </remarks>
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class GetChildrenGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 110;

        [ImportingConstructor]
        public GetChildrenGraphActionHandler(
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(aggregateSnapshotProvider)
        {
        }

        public override bool TryHandleRequest(IGraphContext graphContext)
        {
            return
                graphContext.Direction == GraphContextDirection.Contains &&
                GetChildren();

            bool GetChildren()
            {
                bool trackChanges = false;
                foreach (GraphNode inputGraphNode in graphContext.InputNodes)
                {
                    if (graphContext.CancelToken.IsCancellationRequested)
                    {
                        return trackChanges;
                    }

                    string projectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                    if (string.IsNullOrEmpty(projectPath))
                    {
                        continue;
                    }

                    IDependency dependency = FindDependency(inputGraphNode, out IDependenciesSnapshot snapshot);
                    if (dependency == null || snapshot == null)
                    {
                        continue;
                    }

                    IDependenciesGraphViewProvider viewProvider = FindViewProvider(dependency);

                    if (viewProvider == null)
                    {
                        continue;
                    }

                    if (graphContext.TrackChanges)
                    {
                        trackChanges = true;
                    }

                    using (var scope = new GraphTransactionScope())
                    {
                        viewProvider.BuildGraph(
                            graphContext,
                            projectPath,
                            dependency,
                            inputGraphNode,
                            snapshot.Targets[dependency.TargetFramework]);

                        scope.Complete();
                    }
                }

                return trackChanges;
            }
        }
    }
}
