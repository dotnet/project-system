// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    ///     Handles requests asking whether a given dependencies graph node has any child nodes.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This information can be used to determine whether to show an 'expand' marker
    ///     against graph node items in Solution Explorer.
    /// </para>
    /// <para>
    ///     The actual population of child nodes is performed separately by
    ///     <see cref="GetChildrenGraphActionHandler"/>.
    /// </para>
    /// </remarks>
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class CheckChildrenGraphActionHandler : InputNodeGraphActionHandlerBase
    {
        public const int Order = 100;

        [ImportingConstructor]
        public CheckChildrenGraphActionHandler(
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(aggregateSnapshotProvider)
        {
        }

        protected override bool CanHandle(IGraphContext graphContext)
        {
            return graphContext.Direction == GraphContextDirection.Self &&
                   graphContext.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren);
        }

        protected override void ProcessInputNode(IGraphContext graphContext, GraphNode inputGraphNode, IDependency dependency, DependenciesSnapshot snapshot, IDependenciesGraphViewProvider viewProvider, string projectPath, ref bool trackChanges)
        {
            inputGraphNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, dependency.Id);
            inputGraphNode.SetValue(DependenciesGraphSchema.ResolvedProperty, dependency.Resolved);

            if (viewProvider.HasChildren(dependency))
            {
                inputGraphNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
            }
        }
    }
}
