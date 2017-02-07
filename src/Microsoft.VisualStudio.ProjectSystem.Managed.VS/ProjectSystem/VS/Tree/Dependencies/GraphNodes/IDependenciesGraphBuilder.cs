// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    internal interface IDependenciesGraphBuilder
    {
        GraphNode AddGraphNode(
            IGraphContext graphContext, 
            string projectPath, 
            GraphNode parentNode, 
            IDependency dependency);

        GraphNode AddGraphNode(
            IGraphContext graphContext, 
            string projectPath, 
            GraphNode parentNode, 
            IDependencyViewModel viewModel);

        GraphNode AddTopLevelGraphNode(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency);

        void RemoveGraphNode(
            IGraphContext graphContext,
            string projectPath,
            IDependency dependency,
            GraphNode parentNode);
    }
}
