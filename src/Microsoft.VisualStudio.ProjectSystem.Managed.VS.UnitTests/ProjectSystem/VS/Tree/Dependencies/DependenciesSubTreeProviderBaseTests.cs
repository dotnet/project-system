//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using Microsoft.VisualStudio.Imaging.Interop;
//using Microsoft.VisualStudio.ProjectSystem.Properties;
//using Xunit;

//namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
//{
//    [ProjectSystemTrait]
//    public class DependenciesSubTreeProviderBaseTests
//    {
//        [Fact]
//        public void DependenciesSubTreeProviderBase_Defaults()
//        {
//            var provider = new TestableDependenciesSubTreeProviderBase();

//            // by default is false always
//            Assert.False(provider.IsInErrorState);

//            // by default is false always hide provider's node
//            Assert.False(provider.ShouldBeVisibleWhenEmpty);

//            // by default OriginalItemSpec
//            Assert.Equal("OriginalItemSpec", provider.GetOriginalItemSpecPropertyName());

//            // by default both resolved and unresolved rule names are empty 
//            Assert.False(provider.GetResolvedReferenceRuleNames().Any());
//            Assert.False(provider.GetUnresolvedReferenceRuleNames().Any());
//        }

//        [Fact]
//        public void DependenciesSubTreeProviderBase_ProcessDuplicatedNodes()
//        {
//            // Arrange
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
//        ""ItemSpec"": ""TopNodeItemSpec1""
//    }
//}");
//            topNode1.SetProperties(caption: "Caption1");

//            var topNode2 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""TopNodeItemSpec2""
//    }
//}");
//            topNode2.SetProperties(caption: "Caption2");

//            var topNode3 = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""TopNodeItemSpec3""
//    }
//}");
//            topNode3.SetProperties(caption: "Caption3");

//            topNode2.SetProperties(caption:topNode2.Alias);
//            rootNode.AddChild(topNode1);
//            rootNode.AddChild(topNode2);
//            rootNode.AddChild(topNode3);

//            var dependenciesChange = DependenciesChangeFactory.FromJson(@"
//{
//    ""AddedNodes"": [
//        {
//            ""Id"": {
//                ""ProviderType"": ""MyProvider"",
//                ""ItemSpec"": ""TopNodeItemSpec1"",
//                ""ItemType"": ""OtherItemType""
//            }
//        },
//        {
//            ""Id"": {
//                ""ProviderType"": ""MyProvider"",
//                ""ItemSpec"": ""TopNodeItemSpec2"",
//                ""ItemType"": ""OtherItemType""
//            }
//        }
//    ],    
//    ""UpdatedNodes"": [ ],
//    ""RemovedNodes"": [ ]
//}");
//            var addedNodesArray = dependenciesChange.AddedNodes.ToArray();
//            addedNodesArray[0].SetProperties(caption:"Caption1");
//            addedNodesArray[1].SetProperties(caption:"Caption2");

//            var provider = new TestableDependenciesSubTreeProviderBase();
//            provider.SetRootNode(rootNode);

//            // Act
//            provider.TestProcessDuplicatedNodes(dependenciesChange);

//            // Assert
//            Assert.Equal(1, dependenciesChange.UpdatedNodes.Count);
//            Assert.Equal(topNode1.Alias, topNode1.Caption);
//            Assert.Equal(5, provider.RootNode.Children.Count);
//            var childrenArray = provider.RootNode.Children.ToArray();
//            Assert.Equal(childrenArray[3].Alias, childrenArray[3].Caption);
//            Assert.Equal(childrenArray[4].Alias, childrenArray[4].Caption);
//        }

//        [Fact]
//        public void DependenciesSubTreeProviderBase_ProcessDependenciesChanges()
//        {
//            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
//    ""ProjectChanges"": {
//        ""rulenameResolved"": {
//            ""After"": {
//                ""Items"": {
//                    ""item21"": {
//                        ""OriginalItemSpec"":""item1""
//                    },
//                    ""item22"": {
//                        ""OriginalItemSpec"":""item2""
//                    },
//                    ""itemWithoutOriginalItemSpec"": {
//                    },
//                    ""unresolvedItemTobeRemoved"": {
//                        ""OriginalItemSpec"":""unresolvedItemTobeRemoved""
//                    },
//                },
//                ""RuleName"":""rulenameResolved""
//            },
//            ""Before"": {
//                ""Items"": {
//                    ""resolvedItemTobeRemoved"": {
//                        ""OriginalItemSpec"":""unresolvedItemTobeAddedInsteadOfRemovedResolvedItem""
//                    },
//                    ""resolvedItemTobeRemovedWithNonExistentOriginalItemSpec"": {
//                        ""OriginalItemSpec"":""nonExistentUnresolvedOriginalItemSpec""
//                    },
//                },
//                ""RuleName"":""rulenameResolved""
//            },
//            ""Difference"": {
//                ""AddedItems"": [ ""item21"", ""item22"", ""itemWithoutOriginalItemSpec"", ""itemWithoutPropertiesInAfter"", 
//                                  ""unresolvedItemTobeRemoved"" ],
//                ""ChangedItems"": [ ],
//                ""RemovedItems"": [ ""resolvedItemTobeRemoved"", ""resolvedItemTobeRemovedWithNonExistentOriginalItemSpec"" ],
//                ""AnyChanges"": ""true""
//            },
//        },
//        ""rulenameUnresolved"": {
//            ""After"": {
//                ""Items"": {
//                    ""item1"": {
//                    },
//                    ""item2"": {
//                    },
//                    ""item3"": {
//                    },
//                    ""unresolvedItemTobeAddedInsteadOfRemovedResolvedItem"": {
//                    },
//                    ""unresolvedItemTobeRemoved"": {
//                    }
//                },
//                ""RuleName"":""rulenameUnresolved""
//            },
//            ""Difference"": {
//                ""AddedItems"": [ ""item1"", ""item2"", ""item3"" ],
//                ""ChangedItems"": [ ],
//                ""RemovedItems"": [ ""item4"", ""itemNotInRootNode"", ""itemWithoutPropertiesInAfter"" ],
//                ""AnyChanges"": ""true""
//            },
//        },
//        ""rulenameUnknown"": {
//            ""After"": {
//                ""Items"": {
//                    ""shouldNotApper"": {
//                    }
//                },
//                ""RuleName"":""rulenameUnknown""
//            },
//            ""Difference"": {
//                ""AddedItems"": [ ""shouldNotApper"" ],
//                ""ChangedItems"": [ ],
//                ""RemovedItems"": [ ],
//                ""AnyChanges"": ""true""
//            },
//        }
//    }
//}");
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");

//            var item4Node = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""item4"",
//        ""ItemType"": ""myUnresolvedItemType""
//    }
//}");

//            var resolvedItemTobeRemovedNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""unresolvedItemTobeAddedInsteadOfRemovedResolvedItem"",
//        ""ItemType"": ""myUnresolvedItemType""
//    }
//}");

//            var unresolvedItemTobeRemovedNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""unresolvedItemTobeRemoved"",
//        ""ItemType"": ""myUnresolvedItemType""
//    }
//}");

//            var unresolvedItemTobeRemovedNodeWithNonExistentOriginalItemSpecNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""resolvedItemTobeRemovedWithNonExistentOriginalItemSpec"",
//        ""ItemType"": ""myUnresolvedItemType""
//    }
//}");
//            rootNode.AddChild(item4Node);
//            rootNode.AddChild(resolvedItemTobeRemovedNode);
//            rootNode.AddChild(unresolvedItemTobeRemovedNode);
//            rootNode.AddChild(unresolvedItemTobeRemovedNodeWithNonExistentOriginalItemSpecNode);

//            var catalogs = IProjectCatalogSnapshotFactory.ImplementRulesWithItemTypes(
//                new Dictionary<string, string>
//                {
//                    { "rulenameResolved", "myResolvedItemType" },
//                    { "rulenameUnresolved", "myUnresolvedItemType" },
//                    { "unresolvedItemTobeRemoved", "myUnresolvedItemType" },
//                    { "unresolvedItemTobeAddedInsteadOfRemovedResolvedItem", "myUnresolvedItemType" }
//                });

//            var provider = new TestableDependenciesSubTreeProviderBase();
//            // set itemTypes to process
//            provider.SetResolvedReferenceRuleNames(ImmutableHashSet<string>.Empty.Add("rulenameResolved"));
//            provider.SetUnresolvedReferenceRuleNames(ImmutableHashSet<string>.Empty.Add("rulenameUnresolved"));
//            provider.SetRootNode(rootNode);

//            // Act
//            var resultChanges = provider.TestProcessDependenciesChanges(projectSubscriptionUpdate, catalogs);

//            // Assert
//            Assert.NotNull(resultChanges);
//            Assert.Equal(5, resultChanges.AddedNodes.Count);

//            var addedNodesArray = resultChanges.AddedNodes.ToArray();

//            // unresolved items added first
//            Assert.False(addedNodesArray[0].Properties.ContainsKey("OriginalItemSpec"));
//            Assert.False(addedNodesArray[0].Resolved);
//            Assert.Equal("item3", addedNodesArray[0].Caption);

//            Assert.False(addedNodesArray[1].Properties.ContainsKey("OriginalItemSpec"));
//            Assert.False(addedNodesArray[1].Resolved);
//            Assert.Equal("unresolvedItemTobeAddedInsteadOfRemovedResolvedItem", addedNodesArray[1].Caption);

//            Assert.True(addedNodesArray[2].Properties.ContainsKey("OriginalItemSpec"));
//            Assert.True(addedNodesArray[2].Resolved);
//            Assert.Equal("item1", addedNodesArray[2].Caption);
//            Assert.True(addedNodesArray[3].Properties.ContainsKey("OriginalItemSpec"));
//            Assert.True(addedNodesArray[3].Resolved);
//            Assert.Equal("item2", addedNodesArray[3].Caption);

//            Assert.True(addedNodesArray[4].Properties.ContainsKey("OriginalItemSpec"));
//            Assert.True(addedNodesArray[4].Resolved);
//            Assert.Equal("unresolvedItemTobeRemoved", addedNodesArray[4].Caption);

//            Assert.Equal(4, resultChanges.RemovedNodes.Count);

//            var removedNodesArray = resultChanges.RemovedNodes.ToArray();

//            Assert.Equal(item4Node.Id, removedNodesArray[0].Id);
//            Assert.Equal(resolvedItemTobeRemovedNode.Id, removedNodesArray[1].Id);
//            Assert.Equal(unresolvedItemTobeRemovedNodeWithNonExistentOriginalItemSpecNode.Id, removedNodesArray[2].Id);
//            Assert.Equal(unresolvedItemTobeRemovedNode.Id, removedNodesArray[3].Id);
//        }

//        [Fact]
//        public void DependenciesSubTreeProviderBase_ProcessDependenciesChanges_WhenAnyChangesFalse_ShouldSkip()
//        {
//            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
//    ""ProjectChanges"": {
//        ""rulenameResolved"": {
//            ""After"": {
//                ""Items"": {
//                    ""item21"": {
//                        ""OriginalItemSpec"":""item1""
//                    },
//                    ""item22"": {
//                        ""OriginalItemSpec"":""item2""
//                    },
//                    ""itemWithoutOriginalItemSpec"": {
//                    }
//                },
//                ""RuleName"":""rulenameResolved""
//            },
//            ""Before"": {
//                ""Items"": {
//                    ""resolvedItemTobeRemoved"": {
//                        ""OriginalItemSpec"":""unresolvedItemTobeAddedInsteadOfRemovedResolvedItem""
//                    }
//                },
//                ""RuleName"":""rulenameResolved""
//            },
//            ""Difference"": {
//                ""AddedItems"": [ ""item21"", ""item22"", ""itemWithoutOriginalItemSpec"", ""itemWithoutPropertiesInAfter"" ],
//                ""ChangedItems"": [ ],
//                ""RemovedItems"": [ ""resolvedItemTobeRemoved"" ],
//                ""AnyChanges"": ""false""
//            },
//        },
//        ""rulenameUnresolved"": {
//            ""After"": {
//                ""Items"": {
//                    ""item1"": {
//                    },
//                    ""item2"": {
//                    },
//                    ""item3"": {
//                    },
//                    ""unresolvedItemTobeAddedInsteadOfRemovedResolvedItem"": {
//                    }
//                },
//                ""RuleName"":""rulenameUnresolved""
//            },
//            ""Difference"": {
//                ""AddedItems"": [ ""item1"", ""item2"", ""item3"" ],
//                ""ChangedItems"": [ ],
//                ""RemovedItems"": [ ""item4"", ""itemNotInRootNode"", ""itemWithoutPropertiesInAfter"" ],
//                ""AnyChanges"": ""false""
//            },
//        }
//    }
//}");
//            var rootNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""MyRootNode""
//    }
//}");

//            var item4Node = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""item4"",
//        ""ItemType"": ""myUnresolvedItemType""
//    }
//}");

//            var resolvedItemTobeRemovedNode = IDependencyNodeFactory.FromJson(@"
//{
//    ""Id"": {
//        ""ProviderType"": ""MyProvider"",
//        ""ItemSpec"": ""resolvedItemTobeRemoved"",
//        ""ItemType"": ""myUnresolvedItemType""
//    }
//}");
//            rootNode.AddChild(item4Node);
//            rootNode.AddChild(resolvedItemTobeRemovedNode);

//            var catalogs = IProjectCatalogSnapshotFactory.ImplementRulesWithItemTypes(
//                new Dictionary<string, string>
//                {
//                    { "rulenameResolved", "myResolvedItemType" },
//                    { "rulenameUnresolved", "myUnresolvedItemType" }
//                });

//            var provider = new TestableDependenciesSubTreeProviderBase();
//            // set itemTypes to process
//            provider.SetResolvedReferenceRuleNames(ImmutableHashSet<string>.Empty.Add("rulenameResolved"));
//            provider.SetUnresolvedReferenceRuleNames(ImmutableHashSet<string>.Empty.Add("rulenameUnresolved"));
//            provider.SetRootNode(rootNode);

//            // Act
//            var resultChanges = provider.TestProcessDependenciesChanges(projectSubscriptionUpdate, catalogs);

//            // Assert
//            Assert.NotNull(resultChanges);
//            Assert.Equal(0, resultChanges.AddedNodes.Count);
//            Assert.Equal(0, resultChanges.RemovedNodes.Count);
//        }

//        private class TestableDependenciesSubTreeProviderBase : DependenciesSubTreeProviderBase
//        {
//            public DependenciesChange TestProcessDependenciesChanges(
//                                                    IProjectSubscriptionUpdate projectSubscriptionUpdate,
//                                                    IProjectCatalogSnapshot catalogs)
//            {
//                return ProcessDependenciesChanges(projectSubscriptionUpdate, catalogs);
//            }

//            public void TestProcessDuplicatedNodes(DependenciesChange dependenciesChange)
//            {
//                ProcessDuplicatedNodes(dependenciesChange);
//            }

//            public IDependencyNode TestCreateDependencyNode(string itemSpec, string itemType)
//            {
//                return CreateDependencyNode(itemSpec, itemType);
//            }

//            public IDependencyNode TestCreateRootNode()
//            {
//                return CreateRootNode();
//            }

//            public void SetRootNode(IDependencyNode node)
//            {
//                RootNode = node;
//            }

//            public string GetOriginalItemSpecPropertyName()
//            {
//                return OriginalItemSpecPropertyName;
//            }

//            public ImmutableHashSet<string> GetResolvedReferenceRuleNames()
//            {
//                return ResolvedReferenceRuleNames;
//            }

//            public ImmutableHashSet<string> GetUnresolvedReferenceRuleNames()
//            {
//                return UnresolvedReferenceRuleNames;
//            }

//            public void SetResolvedReferenceRuleNames(ImmutableHashSet<string> ruleNames)
//            {
//                ResolvedReferenceRuleNames = ruleNames;
//            }

//            public void SetUnresolvedReferenceRuleNames(ImmutableHashSet<string> ruleNames)
//            {
//                UnresolvedReferenceRuleNames = ruleNames;
//            }

//            protected override IDependencyNode CreateDependencyNode(string itemSpec,
//                                                                   string itemType,
//                                                                   int priority = 0,
//                                                                   IImmutableDictionary<string, string> properties = null,
//                                                                   bool resolved = true)
//            {
//                return new DependencyNode(new DependencyNodeId("MyProvider", itemSpec, itemType), 
//                                          ProjectTreeFlags.Empty, 
//                                          properties: properties, 
//                                          resolved: resolved);
//            }

//            public override IEnumerable<ImageMoniker> Icons
//            {
//                get
//                {
//                    return null;
//                }
//            }

//            protected override IDependencyNode CreateRootNode()
//            {
//                return null;
//            }

//            public override string ProviderType
//            {
//                get
//                {
//                    return "MyProvider";
//                }
//            }
//        }
//    }
//}
