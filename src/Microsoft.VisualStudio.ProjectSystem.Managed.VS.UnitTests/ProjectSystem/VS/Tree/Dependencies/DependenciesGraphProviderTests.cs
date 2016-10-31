// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependenciesGraphProviderTests
    {
        [Fact]
        public async Task DependenciesGraphProvider_CheckChildrenAsync()
        {
            var projectPath = @"c:\myproject\project.csproj";
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

            var existingNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}");
            var existingChildNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec""
    }   
}");

            existingNode.Children.Add(existingChildNode);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            mockProvider.AddTestDependencyNodes(new[] { existingNode });

            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);
            var mockGraphContext = IGraphContextFactory.ImplementContainsChildren(inputNode);

            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act
            await provider.BeginGetGraphDataAsync(mockGraphContext);

            // Assert
            Assert.True(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
            Assert.NotNull(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            Assert.Equal(existingNode, inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            Assert.True(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
        }

        [Theory]
        [InlineData(true, @"c:\myproject\project.csproj", @"file:///[MyProvider;MyNodeItemSpec]", false)]
        [InlineData(false, @"", @"file:///[MyProvider;MyNodeItemSpec]", false)]
        [InlineData(false, @"c:\myproject\project.csproj", @"", false)]
        [InlineData(false, @"c:\myproject\project.csproj", @"file:///[MyProvider;MyNodeItemSpec]", true)]
        [InlineData(false, @"c:\myproject\project.csproj", @"file:///[MyProvider;MyNodeItemSpec]", false)]
        public async Task DependenciesGraphProvider_CheckChildrenAsync_InvalidNodeData(
                        bool canceledToken, string projectPath, string nodeIdString, bool invalidProvider)
        {
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

            var tcs = new CancellationTokenSource();
            if (canceledToken)
            {
                tcs.Cancel();
            }

            var mockGraphContext = IGraphContextFactory.Implement(tcs.Token,
                                                     new HashSet<GraphNode>() { inputNode },
                                                     GraphContextDirection.Self,
                                                     new List<GraphProperty> { DgmlNodeProperties.ContainsChildren });
            IProjectDependenciesSubTreeProviderMock mockProvider = invalidProvider
                    ? null
                    : new IProjectDependenciesSubTreeProviderMock();
            var mockProjectContextProvider =
                    IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act
            await provider.BeginGetGraphDataAsync(mockGraphContext);

            // Assert
            Assert.False(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
            Assert.Null(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            if (invalidProvider)
            {
                Assert.Null(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty));
            }
        }

        [Fact]
        public async Task DependenciesGraphProvider_CheckChildrenAsync_HasChildrenFalse()
        {
            var projectPath = @"c:\myproject\project.csproj";
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

            var nodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}";

            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            mockProvider.AddTestDependencyNodes(new[] { existingNode });
            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

            var mockGraphContext = IGraphContextFactory.Implement(CancellationToken.None,
                                                     new HashSet<GraphNode>() { inputNode },
                                                     GraphContextDirection.Self,
                                                     new List<GraphProperty> { DgmlNodeProperties.ContainsChildren });

            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act
            await provider.BeginGetGraphDataAsync(mockGraphContext);

            // Assert
            Assert.False(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
            Assert.NotNull(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            Assert.Equal(existingNode, inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            Assert.True(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
        }

        [Fact]
        public async Task DependenciesGraphProvider_GetChildrenAsync()
        {
            var projectPath = @"c:\myproject\project.csproj";
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";

            var nodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}";
            var childNodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec""
    }
}";

            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
            var existingChildNode = IDependencyNodeFactory.FromJson(childNodeJson);
            existingNode.Children.Add(existingChildNode);

            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);
            var outputNodes = new HashSet<GraphNode>();

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            mockProvider.AddTestDependencyNodes(new[] { existingNode });

            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);
            var mockGraphContext = IGraphContextFactory.ImplementGetChildrenAsync(inputNode,
                                                                                  trackChanges: true,
                                                                                  outputNodes: outputNodes);

            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act
            await provider.BeginGetGraphDataAsync(mockGraphContext);

            // Assert
            Assert.Equal(1, outputNodes.Count);
            var childGraphNode = outputNodes.First();
            Assert.Equal(existingChildNode, childGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            Assert.False(childGraphNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
            Assert.True(childGraphNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
            Assert.Equal(1, childGraphNode.IncomingLinkCount);
            Assert.Equal(1, provider.GetRegisteredSubTreeProviders().Count());
        }

        [Theory]
        [InlineData(true, @"c:\myproject\project.csproj", @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}")]
        [InlineData(false, @"", @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}")]
        [InlineData(false, @"c:\myproject\project.csproj", @"")]
        public async Task DependenciesGraphProvider_GetChildrenAsync_InvalidNodeData(
                        bool canceledToken, string projectPath, string nodeJson)
        {
            // Arrange
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var tcs = new CancellationTokenSource();
            if (canceledToken)
            {
                tcs.Cancel();
            }

            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);

            var mockGraphContext = IGraphContextFactory.Implement(tcs.Token,
                                                     new HashSet<GraphNode>() { inputNode },
                                                     GraphContextDirection.Contains);

            var provider = new DependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act (if something is wrong, there would be exception since we did not provide more mocks)
            await provider.BeginGetGraphDataAsync(mockGraphContext);
        }

        [Fact]
        public async Task DependenciesGraphProvider_GetChildrenAsync_NodeDoesNotExistAnyMore()
        {
            // Arrange
            var projectPath = @"c:\myproject\project.csproj";
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var nodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}";
            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);

            var mockGraphContext = IGraphContextFactory.Implement(CancellationToken.None,
                                                     new HashSet<GraphNode>() { inputNode },
                                                     GraphContextDirection.Contains);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            var mockProjectContextProvider =
                    IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act (if something is wrong, there would be exception since we did not provide more mocks)
            await provider.BeginGetGraphDataAsync(mockGraphContext);
        }

        [Fact]
        public async Task DependenciesGraphProvider_GetChildrenAsync_WhenPreFilledFolderNode_ShouldNotRefresh()
        {
            // Arrange
            var projectPath = @"c:\myproject\project.csproj";
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var nodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}";
            var childNodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec""
    }
}";

            var existingNode = IDependencyNodeFactory.FromJson(nodeJson, DependencyNode.PreFilledFolderNode);
            var existingChildNode = IDependencyNodeFactory.FromJson(childNodeJson, DependencyNode.PreFilledFolderNode);

            existingNode.Children.Add(existingChildNode);

            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);
            var outputNodes = new HashSet<GraphNode>();

            var mockGraphContext = IGraphContextFactory.ImplementGetChildrenAsync(inputNode,
                                                                                  trackChanges: true,
                                                                                  outputNodes: outputNodes);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            var mockProjectContextProvider =
                    IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act
            await provider.BeginGetGraphDataAsync(mockGraphContext);

            // Assert
            Assert.Equal(1, outputNodes.Count);
            var childGraphNode = outputNodes.First();
            Assert.Equal(existingChildNode,
                         childGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
            Assert.True(childGraphNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
            Assert.True(childGraphNode.GetValue(
                            DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
            Assert.Equal(1, childGraphNode.IncomingLinkCount);
        }

        [Fact]
        public async Task DependenciesGraphProvider_SearchAsync()
        {
            // Arrange
            var searchString = "1.0";
            var projectPath = @"c:\myproject\project.csproj";
            var rootNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyRootNode""
    }
}");

            var topNode1 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyTopNodeItemSpec""
    }
}");
            var topNode2 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyTopNodeItemSpec/1.0.0""
    }
}");

            var topNode3 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyTopNodeItemSpec3/1.0.0""
    }
}");

            var childNode1 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec1.0""
    }
}");

            var childNode2 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec/1.0.0""
    }
}");

            var childNode3 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec""
    }
}");
            topNode1.Children.Add(childNode1);
            topNode2.Children.Add(childNode3);
            topNode2.Children.Add(childNode2);
            rootNode.AddChild(topNode1);
            rootNode.AddChild(topNode2);
            rootNode.AddChild(topNode3);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            mockProvider.RootNode = rootNode;
            mockProvider.AddSearchResults(new[] { topNode1, topNode2, topNode3 });

            var mockProjectContextProvider =
                    IDependenciesGraphProjectContextProviderFactory.Implement(
                        projectPath, subTreeProviders: new[] { mockProvider });

            var outputNodes = new HashSet<GraphNode>();
            var mockGraphContext = IGraphContextFactory.ImplementSearchAsync(searchString,
                                                                             outputNodes: outputNodes);

            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

            // Act
            await provider.BeginGetGraphDataAsync(mockGraphContext);

            // Assert
            Assert.Equal(5, outputNodes.Count);

            var outputArray = outputNodes.ToArray();
            // check if top level nodes got CodeNodeCategories.ProjectItem to make sure
            // graph matched them back with IVsHierarchy nodes
            Assert.True(outputArray[0].HasCategory(CodeNodeCategories.ProjectItem));
            Assert.Equal(1, outputArray[0].OutgoingLinkCount);
            Assert.True(outputArray[2].HasCategory(CodeNodeCategories.ProjectItem));
            Assert.Equal(2, outputArray[2].OutgoingLinkCount);

            Assert.False(outputArray[1].HasCategory(CodeNodeCategories.ProjectItem));
            Assert.False(outputArray[3].HasCategory(CodeNodeCategories.ProjectItem));
            Assert.False(outputArray[4].HasCategory(CodeNodeCategories.ProjectItem));
        }

        [Fact]
        public async Task DependenciesGraphProvider_TrackChangesAsync()
        {
            var projectPath = @"c:\myproject\project.csproj";
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

            var existingNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}");
            var existingChildNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpecExisting""
    }
}");

            var existingRefreshedNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}");
            var newChildNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpecNew""
    }
}");
            existingNode.Children.Add(existingChildNode);
            existingRefreshedNode.AddChild(newChildNode);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            mockProvider.AddTestDependencyNodes(new[] { existingRefreshedNode });

            inputNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, existingNode);
            inputNode.SetValue(DependenciesGraphSchema.ProviderProperty, mockProvider);

            var outputNodes = new HashSet<GraphNode>();
            var mockGraphContext = IGraphContextFactory.ImplementTrackChanges(inputNode, outputNodes);

            var updatedProjectContext =
                IDependenciesGraphProjectContextProviderFactory.ImplementProjectContext(projectPath);

            var provider = new TestableDependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);
            provider.AddExpandedGraphContext(mockGraphContext);

            // Act
            await provider.TrackChangesAsync(updatedProjectContext);

            // Assert
            Assert.Equal(1, outputNodes.Count);
            var outputNode = outputNodes.First();
            var outputDependency = outputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty);
            Assert.Equal(newChildNode.Id, outputDependency.Id);
        }

        [Theory]
        [InlineData("", true, true)]
        [InlineData(@"c:\myproject\project.csproj", false, true)]
        [InlineData(@"c:\myproject\project.csproj", true, false)]
        public async Task DependenciesGraphProvider_TrackChangesAsync_InvalidNodeData(
                            string projectPath, bool existingNodeSpecified, bool updatedNodeProvided)
        {
            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

            var existingNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}");
            var existingChildNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpecExisting""
    }
}");

            var existingRefreshedNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyNodeItemSpec""
    }
}");
            var newChildNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpecNew""
    }
}");
            existingNode.Children.Add(existingChildNode);
            existingRefreshedNode.AddChild(newChildNode);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            if (updatedNodeProvided)
            {
                mockProvider.AddTestDependencyNodes(new[] { existingRefreshedNode });
            }

            if (existingNodeSpecified)
            {
                inputNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, existingNode);
            }

            inputNode.SetValue(DependenciesGraphSchema.ProviderProperty, mockProvider);

            var outputNodes = new HashSet<GraphNode>();
            var mockGraphContext = IGraphContextFactory.ImplementTrackChanges(inputNode, outputNodes);

            var updatedProjectContext =
                IDependenciesGraphProjectContextProviderFactory.ImplementProjectContext(projectPath);

            var provider = new TestableDependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
                                                         Mock.Of<SVsServiceProvider>(),
                                                         new IProjectThreadingServiceMock().JoinableTaskContext);
            provider.AddExpandedGraphContext(mockGraphContext);

            // Act
            await provider.TrackChangesAsync(updatedProjectContext);

            // Assert
            Assert.Equal(0, outputNodes.Count);
        }

        private class TestableDependenciesGraphProvider : DependenciesGraphProvider
        {
            public TestableDependenciesGraphProvider(
                        IDependenciesGraphProjectContextProvider projectContextProvider,
                        SVsServiceProvider serviceProvider,
                        JoinableTaskContextNode joinableTaskContextNode)
                : base(projectContextProvider, serviceProvider, joinableTaskContextNode)
            {

            }

            public void AddExpandedGraphContext(IGraphContext context)
            {
                ExpandedGraphContexts = new PlatformUI.WeakCollection<IGraphContext>();
                ExpandedGraphContexts.Add(context);
            }

            public IEnumerable<IGraphContext> GetExpandedGraphContexts()
            {
                return ExpandedGraphContexts;
            }

            public HashSet<string> GetRegisteredSubTreeProviders()
            {
                return RegisteredSubTreeProviders;
            }
        }
    }
}
