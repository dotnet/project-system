// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.GraphModel;
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
    internal sealed class GetChildrenGraphActionHandler : InputNodeGraphActionHandlerBase
    {
        public const int Order = 110;

        [ImportingConstructor]
        public GetChildrenGraphActionHandler(
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(aggregateSnapshotProvider)
        {
        }

        protected override bool CanHandle(IGraphContext graphContext)
        {
            return graphContext.Direction == GraphContextDirection.Contains;
        }

        protected override void ProcessInputNode(IGraphContext graphContext, GraphNode inputGraphNode, IDependency dependency, DependenciesSnapshot snapshot, IDependenciesGraphViewProvider viewProvider, string projectPath, ref bool trackChanges)
        {
            if (graphContext.TrackChanges)
            {
                trackChanges = true;
            }

            viewProvider.BuildGraph(
                graphContext,
                projectPath,
                dependency,
                inputGraphNode,
                snapshot.DependenciesByTargetFramework[dependency.TargetFramework]);
        }
    }
}
