// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IGraphContextFactory
    {
        public static IGraphContext Create()
        {
            return Mock.Of<IGraphContext>();
        }

        public static GraphNode CreateNode(string projectPath, 
                                           string nodeIdString, 
                                           IDependencyNode node = null,
                                           IVsHierarchyItem hierarchyItem = null)
        {
            var graph = new Graph();
            var id = GraphNodeId.GetNested(
                        GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(projectPath, UriKind.RelativeOrAbsolute)),
                        GraphNodeId.GetPartial(CodeGraphNodeIdName.File, new Uri(nodeIdString, UriKind.RelativeOrAbsolute))
                    );

            var graphNode = graph.Nodes.GetOrCreate(id);
            if (node != null)
            {
                graphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, node);
            }
            if (hierarchyItem != null)
            {
                graphNode.SetValue(HierarchyGraphNodeProperties.HierarchyItem, hierarchyItem);
            }

            return graphNode;
        }

        public static IGraphContext Implement(CancellationToken? cancellationToken = null,
                                              HashSet<GraphNode> inputNodes = null,
                                              GraphContextDirection? direction = null,
                                              List<GraphProperty> requestedProperties = null,
                                              MockBehavior? mockBehavior = null)
        {
            return ImplementGetMock(cancellationToken, inputNodes, direction, requestedProperties, mockBehavior)
                        .Object;
        }

        private static Mock<IGraphContext> ImplementGetMock(
                                              CancellationToken? cancellationToken = null,
                                              HashSet<GraphNode> inputNodes = null,
                                              GraphContextDirection? direction = null,
                                              List<GraphProperty> requestedProperties = null,
                                              MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IGraphContext>(behavior);

            if (cancellationToken.HasValue)
            {
                mock.Setup(x => x.CancelToken).Returns(cancellationToken.Value);
            }

            if (inputNodes != null)
            {
                mock.Setup(x => x.InputNodes).Returns(inputNodes);
            }

            if (direction.HasValue)
            {
                mock.Setup(x => x.Direction).Returns(direction.Value);
            }

            if (requestedProperties != null)
            {
                mock.Setup(x => x.RequestedProperties).Returns(requestedProperties);
            }

            return mock;
        }

        public static IGraphContext ImplementContainsChildren(GraphNode inputNode)
        {
            var mock = ImplementGetMock(CancellationToken.None,
                                         new HashSet<GraphNode>() { inputNode },
                                         GraphContextDirection.Self,
                                         new List<GraphProperty> { DgmlNodeProperties.ContainsChildren });
            mock.Setup(x => x.OnCompleted());

            return mock.Object;
        }

        public static IGraphContext ImplementGetChildrenAsync(GraphNode inputNode, 
                                                              bool trackChanges,
                                                              HashSet<GraphNode> outputNodes = null)
        {
            var mock = ImplementGetMock(CancellationToken.None,
                                        new HashSet<GraphNode>() { inputNode },
                                        GraphContextDirection.Contains);
            mock.Setup(x => x.OnCompleted());
            mock.Setup(x => x.TrackChanges).Returns(trackChanges);
            mock.Setup(x => x.Graph).Returns(new Graph());
            if (outputNodes == null)
            {
                outputNodes = new HashSet<GraphNode>();
            }

            mock.Setup(x => x.OutputNodes).Returns(outputNodes);

            return mock.Object;
        }

        public static IGraphContext ImplementSearchAsync(string searchString,
                                                         HashSet<GraphNode> outputNodes = null)
        {
            var mock = ImplementGetMock(CancellationToken.None,
                                        direction:GraphContextDirection.Custom);

            var mockSearchQuery = new Mock<IVsSearchQuery>();
            mockSearchQuery.Setup(q => q.SearchString).Returns(searchString);

            var mockSearchParameters = new Mock<ISolutionSearchParameters>();
            mockSearchParameters.Setup(s => s.SearchQuery).Returns(mockSearchQuery.Object);

            mock.Setup(g => g.GetValue<ISolutionSearchParameters>(It.IsAny<string>())).Returns(mockSearchParameters.Object);
            mock.Setup(x => x.OnCompleted());
            mock.Setup(x => x.Graph).Returns(new Graph());

            if (outputNodes == null)
            {
                outputNodes = new HashSet<GraphNode>();
            }

            mock.Setup(x => x.OutputNodes).Returns(outputNodes);

            return mock.Object;
        }

        public static IGraphContext ImplementTrackChanges(GraphNode inputNode,
                                                          HashSet<GraphNode> outputNodes = null)
        {
            var mock = ImplementGetMock(inputNodes: new HashSet<GraphNode>() { inputNode });
            mock.Setup(x => x.OnCompleted());
            mock.Setup(x => x.Graph).Returns(new Graph());

            if (outputNodes == null)
            {
                outputNodes = new HashSet<GraphNode>();
            }

            mock.Setup(x => x.OutputNodes).Returns(outputNodes);

            return mock.Object;
        }
    }
}