//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using Microsoft.VisualStudio.Imaging;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Threading.Tasks;
//using Xunit;

//namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
//{
//    [ProjectSystemTrait]
//    public class SdkDependenciesSubTreeProviderTests
//    {
//        [Fact]
//        public void SdkDependenciesSubTreeProvider_GetDependencyNode()
//        {
//            // Arrange
//            const string packageItemSpec = "MyPackage1";

//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");
//            var existingNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec1""
//    },
//    ""Properties"": {
//        ""SDKPackageItemSpec"": ""MyPackage1""
//    }
//}");

//            var existingNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec2""
//    },
//    ""Properties"": {
//    }
//}");

//            var childNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec1""
//    }   
//}");

//            var childNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec2""
//    },
//    ""Properties"": {
//        ""SDKPackageItemSpec"": ""SomeUnknownPackage""
//    }
//}");
//            rootNode.AddChild(existingNode1);
//            rootNode.AddChild(existingNode2);

//            var childrenToAdd = new[] { childNode1, childNode2 };

//            var nugetPackagesDataProvider =
//                INuGetPackagesDataProviderFactory.ImplementUpdateNodeChildren(packageItemSpec, existingNode1, childrenToAdd);

//            var provider = new TestableSdkDependenciesSubTreeProvider(nugetPackagesDataProvider);
//            provider.SetRootNode(rootNode);

//            // Successful scenario 
//            // Act
//            var resultNode = provider.GetDependencyNode(existingNode1.Id);
//            // Assert
//            Assert.Equal(2, existingNode1.Children.Count);

//            // node does not exist in root
//            // Act
//            resultNode = provider.GetDependencyNode(childNode1.Id);
//            // Assert
//            Assert.Null(resultNode);

//            // node does not have proprty SDKPackageItemSpecProperty
//            // Act
//            resultNode = provider.GetDependencyNode(existingNode2.Id);
//            // Assert
//            Assert.Equal(existingNode2, resultNode);
//        }

//        [Fact]
//        public async Task SdkDependenciesSubTreeProvider_SearchAsync()
//        {
//            // Arrange
//            const string packageItemSpec = "MyPackage1";

//            var existingNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec1""
//    },
//    ""Properties"": {
//        ""SDKPackageItemSpec"": ""MyPackage1""
//    }
//}");

//            var existingNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyNodeItemSpec2""
//    },
//    ""Properties"": {
//    }
//}");

//            var searchResultNode1 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec1""
//    }   
//}");

//            var searchResultNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyChildNodeItemSpec2""
//    },
//    ""Properties"": {
//        ""SDKPackageItemSpec"": ""SomeUnknownPackage""
//    }
//}");

//            var searchResults = new[] { searchResultNode1, searchResultNode2 };

//            var nugetPackagesDataProvider =
//                INuGetPackagesDataProviderFactory.ImplementSearchAsync(packageItemSpec, "xxx", searchResults);

//            var provider = new TestableSdkDependenciesSubTreeProvider(nugetPackagesDataProvider);

//            // Successful scenario 
//            // Act
//            var resultNodes = await provider.SearchAsync(existingNode1, "xxx");
//            // Assert
//            Assert.Equal(2, resultNodes.Count());

//            // node does not have proprty SDKPackageItemSpecProperty
//            // Act
//            resultNodes = await provider.SearchAsync(existingNode2, "xxx");
//            // Assert
//            Assert.Null(resultNodes);
//        }

//        [Fact]
//        public void SdkDependenciesSubTreeProvider_CreateRootNode()
//        {
//            var provider = new TestableSdkDependenciesSubTreeProvider(null);

//            var rootNode = provider.TestCreateRootNode();

//            Assert.True(rootNode is SubTreeRootDependencyNode);
//            Assert.True(rootNode.Flags.Contains(SdkDependenciesSubTreeProvider.SdkSubTreeRootNodeFlags));
//            Assert.Equal("SDK", rootNode.Caption);
//            Assert.Equal(KnownMonikers.BrowserSDK, rootNode.Icon);
//            Assert.Equal(SdkDependenciesSubTreeProvider.ProviderTypeString, rootNode.Id.ProviderType);
//        }

//        [Fact]
//        public void SdkDependenciesSubTreeProvider_CreateDependencyNode()
//        {
//            const string itemSpec = "myItemSpec";
//            const string itemType = "myItemType";
//            const int priority = 15;
//            var properties = new Dictionary<string, string>
//            {
//                { "myproPerty", "myValue" }
//            };
//            const bool resolved = true;

//            var provider = new TestableSdkDependenciesSubTreeProvider(null);

//            var node = provider.TestCreateDependencyNode(itemSpec, itemType, priority, properties, resolved);

//            Assert.True(node is SdkDependencyNode);
//            Assert.True(node.Flags.Contains(SdkDependenciesSubTreeProvider.SdkSubTreeNodeFlags));
//            Assert.False(node.Flags.Contains(DependencyNode.DoesNotSupportRemove));
//            Assert.Equal(itemSpec, node.Caption);
//            Assert.Equal(KnownMonikers.BrowserSDK, node.Icon);
//            Assert.Equal(SdkDependenciesSubTreeProvider.ProviderTypeString, node.Id.ProviderType);
//            Assert.Equal(DependencyNode.SdkNodePriority, node.Priority);
//            Assert.Equal(1, node.Properties.Count);
//            Assert.Equal("myValue", node.Properties["myproPerty"]);
//        }

//        [Fact]
//        public void SdkDependenciesSubTreeProvider_CreateDependencyNode_Implicit()
//        {
//            const string itemSpec = "myItemSpec";
//            const string itemType = "myItemType";
//            const int priority = 15;
//            var properties = new Dictionary<string, string>
//            {
//                { "myproPerty", "myValue" },
//                { SdkReference.SDKPackageItemSpecProperty, "somevalue" }
//            };
//            const bool resolved = true;

//            var provider = new TestableSdkDependenciesSubTreeProvider(null);

//            var node = provider.TestCreateDependencyNode(itemSpec, itemType, priority, properties, resolved);

//            Assert.True(node is SdkDependencyNode);
//            Assert.True(node.Flags.Contains(SdkDependenciesSubTreeProvider.SdkSubTreeNodeFlags));
//            Assert.True(node.Flags.Contains(DependencyNode.DoesNotSupportRemove));
//            Assert.Equal(itemSpec, node.Caption);
//            Assert.Equal(KnownMonikers.BrowserSDK, node.Icon);
//            Assert.Equal(SdkDependenciesSubTreeProvider.ProviderTypeString, node.Id.ProviderType);
//            Assert.Equal(DependencyNode.SdkNodePriority, node.Priority);
//            Assert.Equal(2, node.Properties.Count);
//            Assert.Equal("myValue", node.Properties["myproPerty"]);
//            Assert.Equal("somevalue", node.Properties[SdkReference.SDKPackageItemSpecProperty]);
//        }

//        private class TestableSdkDependenciesSubTreeProvider : SdkDependenciesSubTreeProvider
//        {
//            public TestableSdkDependenciesSubTreeProvider(INuGetPackagesDataProvider nuGetPackagesSnapshotProvider)
//                : base(nuGetPackagesSnapshotProvider)
//            {
//            }

//            public IDependencyNode TestCreateRootNode()
//            {
//                return CreateRootNode();
//            }

//            public IDependencyNode TestCreateDependencyNode(string itemSpec,
//                                                            string itemType,
//                                                            int priority,
//                                                            IDictionary<string, string> properties,
//                                                            bool resolved)
//            {
//                return CreateDependencyNode(itemSpec, itemType, priority, properties.ToImmutableDictionary(), resolved);
//            }

//            public void SetRootNode(IDependencyNode node)
//            {
//                RootNode = node;
//            }
//        }
//    }
//}
