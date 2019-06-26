// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Handles the mechanics of adding and removing nodes from the graph.
    /// Decisions about which nodes to add and where to add them are made by the
    /// <see cref="ViewProviders.IDependenciesGraphViewProvider"/> implementations.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependenciesGraphBuilder
    {
        GraphNode AddGraphNode(
            IGraphContext graphContext,
            string projectPath,
            GraphNode parentNode,
            IDependencyViewModel viewModel);

        GraphNode AddTopLevelGraphNode(
            IGraphContext graphContext,
            string projectPath,
            IDependencyViewModel viewModel);

        void RemoveGraphNode(
            IGraphContext graphContext,
            string projectPath,
            string modelId,
            GraphNode parentNode);
    }
}
