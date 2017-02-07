//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using Microsoft.VisualStudio.GraphModel;
//using Microsoft.VisualStudio.GraphModel.Schemas;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.Threading;
//using Moq;
//using Xunit;
//using Task = System.Threading.Tasks.Task;

//namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
//{
//    [ProjectSystemTrait]
//    public class DependenciesGraphProviderTests
//    {
//        [Fact]
//        public async Task DependenciesGraphProvider_CheckChildrenAsync()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var mockVsHierarchyItem = IVsHierarchyItemFactory.ImplementProperties(text: "MyNodeItemSpec");
//            var inputNode = IGraphContextFactory.CreateNode(projectPath,
//                                                            nodeIdString,
//                                                            hierarchyItem: mockVsHierarchyItem);
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");
//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var existingChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }   
//}");
//            rootNode.AddChild(existingNode);
//            existingNode.AddChild(existingChildNode);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.RootNode = rootNode;
//            mockProvider.AddTestDependencyNodes(new[] { existingNode });

//            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);
//            var mockGraphContext = IGraphContextFactory.ImplementContainsChildren(inputNode);

//            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.True(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            Assert.NotNull(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.Equal(existingNode, inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.True(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_CheckChildrenAsync_TopLevelNodeWithoutId_ShouldGetProviderFromParent()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var parentNodeIdString = @"file:///[MyProvider;;;]";
//            var nodeFilePath = @"c:/myproject/MyNodeItemSpec";
//            var mockVsHierarchyItem = IVsHierarchyItemFactory.ImplementProperties(
//                                        text: "MyNodeItemSpec",
//                                        parentCanonicalName: parentNodeIdString);
//            var inputNode = IGraphContextFactory.CreateNode(projectPath,
//                                                            nodeFilePath,
//                                                            hierarchyItem: mockVsHierarchyItem);
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");
//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var existingChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }   
//}");
//            rootNode.AddChild(existingNode);
//            existingNode.AddChild(existingChildNode);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.RootNode = rootNode;
//            mockProvider.AddTestDependencyNodes(new[] { existingNode });

//            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);
//            var mockGraphContext = IGraphContextFactory.ImplementContainsChildren(inputNode);

//            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.True(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            Assert.NotNull(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.Equal(existingNode, inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.True(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
//        }

//        [Theory]
//        [InlineData(true, @"c:\myproject\project.csproj", @"file:///[MyProvider;MyNodeItemSpec]", false)]
//        [InlineData(false, @"", @"file:///[MyProvider;MyNodeItemSpec]", false)]
//        [InlineData(false, @"c:\myproject\project.csproj", @"", false)]
//        [InlineData(false, @"c:\myproject\project.csproj", @"file:///[MyProvider;MyNodeItemSpec]", true)]
//        [InlineData(false, @"c:\myproject\project.csproj", @"file:///[MyProvider;MyNodeItemSpec]", false)]
//        public async Task DependenciesGraphProvider_CheckChildrenAsync_InvalidNodeData(
//                        bool canceledToken, string projectPath, string nodeIdString, bool invalidProvider)
//        {
//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

//            var tcs = new CancellationTokenSource();
//            if (canceledToken)
//            {
//                tcs.Cancel();
//            }

//            var mockGraphContext = IGraphContextFactory.Implement(tcs.Token,
//                                                     new HashSet<GraphNode>() { inputNode },
//                                                     GraphContextDirection.Self,
//                                                     new List<GraphProperty> { DgmlNodeProperties.ContainsChildren });
//            IProjectDependenciesSubTreeProviderMock mockProvider = invalidProvider
//                    ? null
//                    : new IProjectDependenciesSubTreeProviderMock();
//            var mockProjectContextProvider =
//                    IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

//            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.False(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            Assert.Null(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            if (invalidProvider)
//            {
//                Assert.Null(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty));
//            }
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_CheckChildrenAsync_HasChildrenFalse()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";

//            var mockVsHierarchyItem = IVsHierarchyItemFactory.ImplementProperties(text: "MyNodeItemSpec");
//            var inputNode = IGraphContextFactory.CreateNode(projectPath,
//                                                            nodeIdString,
//                                                            hierarchyItem: mockVsHierarchyItem);
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");
//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            rootNode.AddChild(existingNode);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.RootNode = rootNode;
//            mockProvider.AddTestDependencyNodes(new[] { existingNode });

//            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

//            var mockGraphContext = IGraphContextFactory.Implement(CancellationToken.None,
//                                                     new HashSet<GraphNode>() { inputNode },
//                                                     GraphContextDirection.Self,
//                                                     new List<GraphProperty> { DgmlNodeProperties.ContainsChildren });

//            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.False(inputNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            Assert.NotNull(inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.Equal(existingNode, inputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.True(inputNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_GetChildrenAsync()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";

//            var nodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}";
//            var childNodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }
//}";

//            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
//            var existingChildNode = IDependencyNodeFactory.FromJson(childNodeJson);
//            existingNode.AddChild(existingChildNode);

//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);
//            var outputNodes = new HashSet<GraphNode>();

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.AddTestDependencyNodes(new[] { existingNode });

//            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);
//            var mockGraphContext = IGraphContextFactory.ImplementGetChildrenAsync(inputNode,
//                                                                                  trackChanges: true,
//                                                                                  outputNodes: outputNodes);

//            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(1, outputNodes.Count);
//            var childGraphNode = outputNodes.First();
//            Assert.Equal(existingChildNode, childGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.False(childGraphNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            var childProjectPath = childGraphNode.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.Assembly);
//            Assert.Equal(projectPath.Replace('\\', '/'), childProjectPath.AbsolutePath);
//            var childSubTreeProvider = childGraphNode.GetValue(DependenciesGraphSchema.ProviderProperty);
//            Assert.True(childSubTreeProvider is IProjectDependenciesSubTreeProviderMock);
//            Assert.Equal("MyDefaultTestProvider", ((IProjectDependenciesSubTreeProviderMock)childSubTreeProvider).ProviderTestType);
//            Assert.Equal(1, childGraphNode.IncomingLinkCount);
//            Assert.Equal(1, provider.GetRegisteredSubTreeProviders().Count());
//        }

//        [Theory]
//        [InlineData(true, @"c:\myproject\project.csproj", @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}")]
//        [InlineData(false, @"", @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}")]
//        [InlineData(false, @"c:\myproject\project.csproj", @"")]
//        public async Task DependenciesGraphProvider_GetChildrenAsync_InvalidNodeData_NoId(
//                        bool canceledToken, string projectPath, string nodeJson)
//        {
//            // Arrange
//            var nodeIdString = @"";
//            var tcs = new CancellationTokenSource();
//            if (canceledToken)
//            {
//                tcs.Cancel();
//            }

//            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);

//            var mockGraphContext = IGraphContextFactory.Implement(tcs.Token,
//                                                     new HashSet<GraphNode>() { inputNode },
//                                                     GraphContextDirection.Contains);

//            var provider = new DependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act (if something is wrong, there would be exception since we did not provide more mocks)
//            await provider.BeginGetGraphDataAsync(mockGraphContext);
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_GetChildrenAsync_NoNodeAttachedToInputNode_ShouldDiscoverItAgain()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");
//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var existingChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }
//}");
//            rootNode.AddChild(existingNode);
//            existingNode.AddChild(existingChildNode);

//            var mockVsHierarchyItem = IVsHierarchyItemFactory.ImplementProperties(text: "MyNodeItemSpec");
//            var inputNode = IGraphContextFactory.CreateNode(projectPath,
//                                                            nodeIdString,
//                                                            hierarchyItem: mockVsHierarchyItem);
//            var outputNodes = new HashSet<GraphNode>();

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.RootNode = rootNode;
//            mockProvider.AddTestDependencyNodes(new[] { existingNode });

//            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);
//            var mockGraphContext = IGraphContextFactory.ImplementGetChildrenAsync(inputNode,
//                                                                                  trackChanges: true,
//                                                                                  outputNodes: outputNodes);

//            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(1, outputNodes.Count);
//            var childGraphNode = outputNodes.First();
//            Assert.Equal(existingChildNode, childGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.False(childGraphNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            Assert.True(childGraphNode.GetValue(DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
//            Assert.Equal(1, childGraphNode.IncomingLinkCount);
//            Assert.Equal(1, provider.GetRegisteredSubTreeProviders().Count());
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_GetChildrenAsync_NodeDoesNotExistAnyMore()
//        {
//            // Arrange
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var nodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}";
//            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);

//            var mockGraphContext = IGraphContextFactory.Implement(CancellationToken.None,
//                                                     new HashSet<GraphNode>() { inputNode },
//                                                     GraphContextDirection.Contains);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            var mockProjectContextProvider =
//                    IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

//            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act (if something is wrong, there would be exception since we did not provide more mocks)
//            await provider.BeginGetGraphDataAsync(mockGraphContext);
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_GetChildrenAsync_WhenPreFilledFolderNode_ShouldNotRefresh()
//        {
//            // Arrange
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var nodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}";
//            var childNodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }
//}";

//            var existingNode = IDependencyNodeFactory.FromJson(nodeJson, DependencyNode.PreFilledFolderNode);
//            var existingChildNode = IDependencyNodeFactory.FromJson(childNodeJson, DependencyNode.PreFilledFolderNode);

//            existingNode.AddChild(existingChildNode);

//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);
//            var outputNodes = new HashSet<GraphNode>();

//            var mockGraphContext = IGraphContextFactory.ImplementGetChildrenAsync(inputNode,
//                                                                                  trackChanges: true,
//                                                                                  outputNodes: outputNodes);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            var mockProjectContextProvider =
//                    IDependenciesGraphProjectContextProviderFactory.Implement(projectPath, mockProvider);

//            var provider = new DependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(1, outputNodes.Count);
//            var childGraphNode = outputNodes.First();
//            Assert.Equal(existingChildNode,
//                         childGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.True(childGraphNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            Assert.True(childGraphNode.GetValue(
//                            DependenciesGraphSchema.ProviderProperty) is IProjectDependenciesSubTreeProviderMock);
//            Assert.Equal(1, childGraphNode.IncomingLinkCount);
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_GetChildrenAsync_UseContextProjectPathForChildren()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var contextProjectPath = @"c:\mycontextproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";

//            var nodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}";
//            var childNodeJson = @"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }
//}";

//            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
//            existingNode.Id.ContextProject = contextProjectPath;
//            var existingChildNode = IDependencyNodeFactory.FromJson(childNodeJson);
//            existingNode.AddChild(existingChildNode);

//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString, existingNode);
//            var outputNodes = new HashSet<GraphNode>();

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            var mockContextProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockContextProvider.ProviderTestType = "MyContextProvider";
//            mockProvider.AddTestDependencyNodes(new[] { existingNode });

//            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory
//                .ImplementMultipleProjects(new Dictionary<string, IProjectDependenciesSubTreeProvider>
//                {
//                    { projectPath, mockProvider },
//                    { contextProjectPath, mockContextProvider }
//                });

//            var mockGraphContext = IGraphContextFactory.ImplementGetChildrenAsync(inputNode,
//                                                                                  trackChanges: true,
//                                                                                  outputNodes: outputNodes);

//            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(1, outputNodes.Count);
//            var childGraphNode = outputNodes.First();
//            Assert.Equal(existingChildNode, childGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty));
//            Assert.False(childGraphNode.GetValue<bool>(DgmlNodeProperties.ContainsChildren));
//            var childProjectPath = childGraphNode.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.Assembly);
//            Assert.Equal(projectPath.Replace('\\', '/'), childProjectPath.AbsolutePath);
//            var childSubTreeProvider = childGraphNode.GetValue(DependenciesGraphSchema.ProviderProperty);
//            Assert.True(childSubTreeProvider is IProjectDependenciesSubTreeProviderMock);
//            Assert.Equal("MyDefaultTestProvider", ((IProjectDependenciesSubTreeProviderMock)childSubTreeProvider).ProviderTestType);
//            Assert.Equal(1, childGraphNode.IncomingLinkCount);
//            Assert.Equal(1, provider.GetRegisteredSubTreeProviders().Count());
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_SearchAsync()
//        {
//            // Arrange
//            var searchString = "1.0";
//            var projectPath = @"c:\myproject\project.csproj";
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");

//            var topNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyTopNodeItemSpec""
//    }
//}");
//            var topNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyTopNodeItemSpec/1.0.0""
//    }
//}");

//            var topNode3 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyTopNodeItemSpec3/1.0.0""
//    }
//}");

//            var childNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec1.0""
//    }
//}");

//            var childNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec/1.0.0""
//    }
//}");

//            var childNode3 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec""
//    }
//}");
//            topNode1.AddChild(childNode1);
//            topNode2.AddChild(childNode3);
//            topNode2.AddChild(childNode2);
//            rootNode.AddChild(topNode1);
//            rootNode.AddChild(topNode2);
//            rootNode.AddChild(topNode3);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock
//            {
//                RootNode = rootNode
//            };
//            mockProvider.AddTestDependencyNodes(new[] { topNode1, topNode2, topNode3 });
//            mockProvider.AddSearchResults(new[] { topNode1, topNode2, topNode3 });

//            var mockProjectContextProvider =
//                    IDependenciesGraphProjectContextProviderFactory.Implement(
//                        projectPath, subTreeProviders: new[] { mockProvider });

//            var outputNodes = new HashSet<GraphNode>();
//            var mockGraphContext = IGraphContextFactory.ImplementSearchAsync(searchString,
//                                                                             outputNodes: outputNodes);

//            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(4, outputNodes.Count);

//            var topNode1Result = GetNodeById(projectPath, outputNodes, topNode1.Id);
//            Assert.True(topNode1Result.HasCategory(CodeNodeCategories.ProjectItem));
//            Assert.Equal(1, topNode1Result.OutgoingLinkCount);

//            var topNode2Result = GetNodeById(projectPath, outputNodes, topNode2.Id);
//            Assert.Equal(1, topNode2Result.OutgoingLinkCount);
//            Assert.True(topNode2Result.HasCategory(CodeNodeCategories.ProjectItem));

//            var childNode1Result = GetNodeById(projectPath, outputNodes, childNode1.Id);
//            Assert.Equal(0, childNode1Result.OutgoingLinkCount);
//            Assert.False(childNode1Result.HasCategory(CodeNodeCategories.ProjectItem));

//            var childNode2Result = GetNodeById(projectPath, outputNodes, childNode2.Id);
//            Assert.Equal(0, childNode2Result.OutgoingLinkCount);
//            Assert.False(childNode2Result.HasCategory(CodeNodeCategories.ProjectItem));
//        }

//        private GraphNode GetNodeById(string projectPath, IEnumerable<GraphNode> nodes, DependencyNodeId id)
//        {
//            var projectFolder = Path.GetDirectoryName(projectPath);
//            foreach (var node in nodes)
//            {
//                var value = node.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);

//                // for idPartName == CodeGraphNodeIdName.File it can be null, avoid unnecessary exception
//                if (value == null)
//                {
//                    continue;
//                }

//                var idString = (value.IsAbsoluteUri ? value.LocalPath : value.ToString()).Trim('/');
//                var nodeId = DependencyNodeId.FromString(idString);
//                if (nodeId != null && nodeId.Equals(id))
//                {
//                    return node;
//                } 
//                else
//                {
//                    var topLevelId = Path.Combine(projectFolder, id.ItemSpec).Replace('/', '\\');
//                    if (topLevelId.Equals(idString, StringComparison.OrdinalIgnoreCase))
//                    {
//                        return node;
//                    }
//                }
//            }

//            return null;
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_SearchAsync_TopLevel_GenericNode_WithCustomItemSpec()
//        {
//            // Arrange
//            var searchString = "1.0";
//            var projectPath = @"c:\myproject\project.csproj";
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");

//            var topNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyTopNodeItemSpec""
//    }
//}", DependencyNode.GenericDependencyFlags.Union(DependencyNode.CustomItemSpec));
//            ((DependencyNode)topNode1).Name = "TopNodeName";
//            var childNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec1.0""
//    }
//}");
//            topNode1.AddChild(childNode1);
//            rootNode.AddChild(topNode1);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.RootNode = rootNode;
//            mockProvider.AddTestDependencyNodes(new[] { topNode1 });
//            mockProvider.AddSearchResults(new[] { topNode1 });

//            var mockProjectContextProvider =
//                    IDependenciesGraphProjectContextProviderFactory.Implement(
//                        projectPath, subTreeProviders: new[] { mockProvider });

//            var outputNodes = new HashSet<GraphNode>();
//            var mockGraphContext = IGraphContextFactory.ImplementSearchAsync(searchString,
//                                                                             outputNodes: outputNodes);

//            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(2, outputNodes.Count);

//            var outputArray = outputNodes.ToArray();
//            // check if top level nodes got CodeNodeCategories.ProjectItem to make sure
//            // graph matched them back with IVsHierarchy nodes
//            Assert.True(outputArray[0].HasCategory(CodeNodeCategories.ProjectItem));
//            Assert.Equal(1, outputArray[0].OutgoingLinkCount);
//            Assert.Equal(@"c:/myproject/topnodename", outputArray[0].Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File).AbsolutePath);
//            Assert.False(outputArray[1].HasCategory(CodeNodeCategories.ProjectItem));
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_SearchAsync_TopLevel_GenericNode_WithNormalItemSpec()
//        {
//            // Arrange
//            var searchString = "1.0";
//            var projectPath = @"c:\myproject\project.csproj";
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");

//            var topNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyTopNodeItemSpec""
//    },
//    ""Name"":""TopNodeName""
//}", DependencyNode.GenericDependencyFlags);

//            var childNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec1.0""
//    }
//}");
//            topNode1.AddChild(childNode1);
//            rootNode.AddChild(topNode1);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.RootNode = rootNode;
//            mockProvider.AddTestDependencyNodes(new[] { topNode1 });
//            mockProvider.AddSearchResults(new[] { topNode1 });

//            var mockProjectContextProvider =
//                    IDependenciesGraphProjectContextProviderFactory.Implement(
//                        projectPath, subTreeProviders: new[] { mockProvider });

//            var outputNodes = new HashSet<GraphNode>();
//            var mockGraphContext = IGraphContextFactory.ImplementSearchAsync(searchString,
//                                                                             outputNodes: outputNodes);

//            var provider = new TestableDependenciesGraphProvider(mockProjectContextProvider,
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);

//            // Act
//            await provider.BeginGetGraphDataAsync(mockGraphContext);

//            // Assert
//            Assert.Equal(2, outputNodes.Count);

//            var outputArray = outputNodes.ToArray();
//            // check if top level nodes got CodeNodeCategories.ProjectItem to make sure
//            // graph matched them back with IVsHierarchy nodes
//            Assert.True(outputArray[0].HasCategory(CodeNodeCategories.ProjectItem));
//            Assert.Equal(1, outputArray[0].OutgoingLinkCount);
//            Assert.Equal(@"c:/myproject/mytopnodeitemspec", outputArray[0].Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File).AbsolutePath);
//            Assert.False(outputArray[1].HasCategory(CodeNodeCategories.ProjectItem));
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_TrackChangesAsync()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var existingChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpecExisting""
//    }
//}");

//            var existingRefreshedNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var newChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpecNew""
//    }
//}");
//            existingNode.AddChild(existingChildNode);
//            existingRefreshedNode.AddChild(newChildNode);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.AddTestDependencyNodes(new[] { existingRefreshedNode });

//            inputNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, existingNode);
//            inputNode.SetValue(DependenciesGraphSchema.ProviderProperty, mockProvider);

//            var outputNodes = new HashSet<GraphNode>();
//            var mockGraphContext = IGraphContextFactory.ImplementTrackChanges(inputNode, outputNodes);

//            var updatedProjectContext =
//                IDependenciesGraphProjectContextProviderFactory.ImplementProjectContext(projectPath);

//            var provider = new TestableDependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);
//            provider.AddExpandedGraphContext(mockGraphContext);

//            var changes = new ProjectContextEventArgs(updatedProjectContext);

//            // Act
//            await provider.TrackChangesAsync(changes);

//            // Assert
//            Assert.Equal(1, outputNodes.Count);
//            var outputNode = outputNodes.First();
//            var outputDependency = outputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty);
//            Assert.Equal(newChildNode.Id, outputDependency.Id);
//        }

//        [Fact]
//        public async Task DependenciesGraphProvider_TrackChangesAsync_WithContextProject()
//        {
//            var projectPath = @"c:\myproject\project.csproj";
//            var contextProjectPath = @"c:\mycontextproject\project.csproj";
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}", DependencyNode.DependsOnOtherProviders);
//            existingNode.Id.ContextProject = contextProjectPath;
//            var existingChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpecExisting""
//    }
//}");

//            var existingRefreshedNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}", DependencyNode.DependsOnOtherProviders);
//            existingRefreshedNode.Id.ContextProject = contextProjectPath;

//            var newChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpecNew""
//    }
//}");
//            existingNode.AddChild(existingChildNode);
//            existingRefreshedNode.AddChild(newChildNode);

//            var diff = IDependenciesChangeDiffFactory.Implement(new[] { newChildNode },
//                    new IDependencyNode[] { }, new IDependencyNode[] { });

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            mockProvider.AddTestDependencyNodes(new[] { existingRefreshedNode });

//            inputNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, existingNode);
//            inputNode.SetValue(DependenciesGraphSchema.ProviderProperty, mockProvider);

//            var outputNodes = new HashSet<GraphNode>();
//            var mockGraphContext = IGraphContextFactory.ImplementTrackChanges(inputNode, outputNodes);

//            var updatedProjectContext =
//                IDependenciesGraphProjectContextProviderFactory.ImplementProjectContext(contextProjectPath);

//            var provider = new TestableDependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);
//            provider.AddExpandedGraphContext(mockGraphContext);
//            var changes = new ProjectContextEventArgs(updatedProjectContext, diff);

//            // Act
//            await provider.TrackChangesAsync(changes);

//            // Assert
//            Assert.Equal(1, outputNodes.Count);
//            var outputNode = outputNodes.First();
//            var outputDependency = outputNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty);
//            Assert.Equal(newChildNode.Id, outputDependency.Id);
//            var childProjectPath = outputNode.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.Assembly);
//            Assert.Equal(projectPath.Replace('\\', '/'), childProjectPath.AbsolutePath);
//        }

//        [Theory]
//        [InlineData("", true, true)]
//        [InlineData(@"c:\myproject\project.csproj", false, true)]
//        [InlineData(@"c:\myproject\project.csproj", true, false)]
//        public async Task DependenciesGraphProvider_TrackChangesAsync_InvalidNodeData(
//                            string projectPath, bool existingNodeSpecified, bool updatedNodeProvided)
//        {
//            var nodeIdString = @"file:///[MyProvider;MyNodeItemSpec]";
//            var inputNode = IGraphContextFactory.CreateNode(projectPath, nodeIdString);

//            var existingNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var existingChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpecExisting""
//    }
//}");

//            var existingRefreshedNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec""
//    }
//}");
//            var newChildNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpecNew""
//    }
//}");
//            existingNode.AddChild(existingChildNode);
//            existingRefreshedNode.AddChild(newChildNode);

//            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
//            if (updatedNodeProvided)
//            {
//                mockProvider.AddTestDependencyNodes(new[] { existingRefreshedNode });
//            }

//            if (existingNodeSpecified)
//            {
//                inputNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, existingNode);
//            }

//            inputNode.SetValue(DependenciesGraphSchema.ProviderProperty, mockProvider);

//            var outputNodes = new HashSet<GraphNode>();
//            var mockGraphContext = IGraphContextFactory.ImplementTrackChanges(inputNode, outputNodes);

//            var updatedProjectContext =
//                IDependenciesGraphProjectContextProviderFactory.ImplementProjectContext(projectPath);

//            var provider = new TestableDependenciesGraphProvider(IDependenciesGraphProjectContextProviderFactory.Create(),
//                                                         Mock.Of<SVsServiceProvider>(),
//                                                         new IProjectThreadingServiceMock().JoinableTaskContext);
//            provider.AddExpandedGraphContext(mockGraphContext);
//            var changes = new ProjectContextEventArgs(updatedProjectContext);

//            // Act
//            await provider.TrackChangesAsync(changes);

//            // Assert
//            Assert.Equal(0, outputNodes.Count);
//        }

//        private class TestableDependenciesGraphProvider : DependenciesGraphProvider
//        {
//            public TestableDependenciesGraphProvider(
//                        IDependenciesGraphProjectContextProvider projectContextProvider,
//                        SVsServiceProvider serviceProvider,
//                        JoinableTaskContextNode joinableTaskContextNode)
//                : base(projectContextProvider, serviceProvider, joinableTaskContextNode)
//            {

//            }

//            /// <summary>
//            /// Holds a hard reference to the IGraphContext stored in weak collection ExpandedGraphContexts,
//            /// to avoid random GC cleans during unit tests execution.
//            /// </summary>
//            private IGraphContext ContextHolder { get; set; }

//            public void AddExpandedGraphContext(IGraphContext context)
//            {
//                ContextHolder = context;
//                ExpandedGraphContexts = new PlatformUI.WeakCollection<IGraphContext>
//                {
//                    context
//                };
//            }

//            public IEnumerable<IGraphContext> GetExpandedGraphContexts()
//            {
//                return ExpandedGraphContexts;
//            }

//            public HashSet<string> GetRegisteredSubTreeProviders()
//            {
//                return RegisteredSubTreeProviders;
//            }
//        }
//    }
//}
