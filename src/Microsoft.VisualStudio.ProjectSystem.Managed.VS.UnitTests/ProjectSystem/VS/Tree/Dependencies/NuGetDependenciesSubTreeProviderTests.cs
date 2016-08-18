// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class NuGetDependenciesSubTreeProviderTests
    {
        [Theory]
        /* 
            Tests 
               - added items 
               - removed items
               - changed items and verifies that properties were updated in CurrentSnapshot
         */
        [InlineData(
@"{
    ""ProjectChanges"": {
        ""itemtype1"": {
            ""After"": {
                ""Items"": {
                    ""tfm1"": {
                        ""RuntimeIdentifier"": ""net45"",
                        ""TargetFrameworkMoniker"": "".NetFramework,Version=v4.5"",
                        ""FrameworkName"": ""net45"",
                        ""FrameworkVersion"": ""4.5"",
                        ""Dependencies"":""package1/1.0.2.0""
                    },
                    ""tfm1/package1/1.0.2.0"": {
                        ""Name"": ""package"",
                        ""Version"": ""1.0.2.0"",
                        ""Type"":""Package"",
                        ""Path"":""SomePath"",
                        ""Resolved"":""true"",
                        ""Dependencies"":""""
                    },
                    ""tfm1/PackageToRemove/1.0.0"": {
                        ""Name"": ""PackageToRemove"",
                        ""Version"": ""1.0.0"",
                        ""Type"":""Package"",
                        ""Path"":""SomePath"",
                        ""Resolved"":""true"",
                        ""Dependencies"":""""
                    },
                    ""tfm1/PackageToChange/2.0.0"": {
                        ""Name"": ""PackageToChange"",
                        ""Version"": ""2.0.0"",
                        ""Type"":""Package"",
                        ""Path"":""SomePath"",
                        ""Resolved"":""true"",
                        ""Dependencies"":""""
                    }
                }
            },
            ""Difference"": {
                ""AddedItems"": [ ""tfm1"", ""tfm1/package1/1.0.2.0"" ],
                ""ChangedItems"": [ ""tfm1/PackageToChange/2.0.0"", ""tfm1/PackageToChange2/2.0.0"" ],
                ""RemovedItems"": [ ""tfm1/PackageToRemove/1.0.0"" ],
                ""AnyChanges"": ""true""
            },
        },
        ""itemtype2"": {
            ""After"": {
                ""Items"": {
                    ""tfm2"": {
                        ""RuntimeIdentifier"": ""net45"",
                        ""TargetFrameworkMoniker"": "".NetFramework,Version=v4.5"",
                        ""FrameworkName"": ""net45"",
                        ""FrameworkVersion"": ""4.5"",
                        ""Dependencies"":""package1/1.0.2.0""
                    },
                    ""tfm1/package2/1.0.2.0"": {
                        ""Name"": ""package2"",
                        ""Version"": ""1.0.2.0"",
                        ""Type"":""Package"",
                        ""Path"":""SomePath2"",
                        ""Resolved"":""true"",
                        ""Dependencies"":""""
                    }
                }
            },
            ""Difference"": {
                ""AddedItems"": [ ""tfm2"", ""tfm1/package2/1.0.2.0"" ],
                ""ChangedItems"": [ ],
                ""RemovedItems"": [  ],
                ""AnyChanges"": ""false""
            },
        }
    }
}",
@"
{
    ""Nodes"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/PackageToRemove/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        },
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/PackageToChange/2.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ]  
}",
/* package spec for package to be changed, used to verify that property was updated */
"tfm1/PackageToChange/2.0.0",
@"
{
    ""AddedNodes"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/package1/1.0.2.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ],    
    ""UpdatedNodes"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/PackageToChange/2.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ],
    ""RemovedNodes"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/PackageToRemove/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ]
}")]
        public void NuGetDependenciesSubTreeProvider_ProcessDependenciesChanges(
                        string projectSubscriptionUpdateJson,
                        string existingTopLevelNodesJson,
                        string packageToTestVersionUpdate,
                        string existingDependenciesChanges)
        {
            // Arrange
            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(projectSubscriptionUpdateJson);
            var mockRootNode = IDependencyNodeFactory.Implement(existingTopLevelNodesJson);

            var provider = new TestableNuGetDependenciesSubTreeProvider();
            provider.SetRootNode(mockRootNode);

            // Act
            var resultDependenciesChange = provider.TestDependenciesChanged(projectSubscriptionUpdate, catalogs: null);

            // Assert
            // Check that for updated/changed nodes, properties were updated  
            var propertyToCheck = "Version";
            var itemsProperties = projectSubscriptionUpdate.ProjectChanges.Values
                                                            .Where(y => y.Difference.AnyChanges)
                                                            .Select(x => x.After.Items)
                                                            .FirstOrDefault();
            var expectedPropertyValue = itemsProperties[packageToTestVersionUpdate][propertyToCheck];

            var properties = provider.GetDependencyProperties(packageToTestVersionUpdate);
            Assert.Equal(expectedPropertyValue, properties[propertyToCheck]);

            // check that DependenciesChange returned is as expected
            var expectedResult = DependenciesChangeFactory.FromJson(existingDependenciesChanges);
            Assert.True(DependenciesChangeFactory.AreEqual(expectedResult, resultDependenciesChange));

            // Check if all added items were added to Snapshot
            var currentSnapshot = provider.GetCurrentSnapshotDependenciesWorld();
            foreach(var addedNode in expectedResult.AddedNodes)
            {
                Assert.True(currentSnapshot.Any(x => x.Equals(addedNode.Id.ItemSpec, StringComparison.OrdinalIgnoreCase)));
            }
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_CreateRootNode()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var rootNode = provider.TestCreateRootNode();

            Assert.True(rootNode is SubTreeRootDependencyNode);
            Assert.True(rootNode.Flags.Contains(NuGetDependenciesSubTreeProvider.NuGetSubTreeRootNodeFlags));
            Assert.Equal("NuGet", rootNode.Caption);
            Assert.Equal(KnownMonikers.PackageReference, rootNode.Icon);
            Assert.Equal(NuGetDependenciesSubTreeProvider.ProviderTypeString, rootNode.Id.ProviderType);
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_GetDependencyNode_WhenNodeIsInCacheJustReturn()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var nodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""NuGetDependency"",
        ""ItemSpec"": ""tfm1/PackageToRemove/1.0.0"",
        ""ItemType"": ""PackageReference""
    }
}";
            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);
            provider.SetCurrentSnapshotNodesCache(new[] { existingNode });

            var resultNode = provider.GetDependencyNode(existingNode.Id);

            Assert.Equal(existingNode, resultNode);
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_GetDependencyNode_WhenNodeIsNotInSnapshotReturnNull()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var nodeJson = @"
{
    ""Id"": {
        ""ProviderType"": ""NuGetDependency"",
        ""ItemSpec"": ""tfm1/PackageToRemove/1.0.0"",
        ""ItemType"": ""PackageReference""
    }
}";
            var existingNode = IDependencyNodeFactory.FromJson(nodeJson);

            var resultNode = provider.GetDependencyNode(existingNode.Id);

            Assert.Null(resultNode);
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_GetDependencyNode_VerifyAllNodeTypesAreCreated()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var snapshotJson = @"
{
    ""NodesCache"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/Package1/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ],
    ""DependenciesWorld"": [
        {
            ""ItemSpec"": ""tfm1/Package2/1.0.0"",
            ""Properties"": {
                ""Name"": ""Package2"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": ""Package3/2.0.0;NotExistentPackage/2.0.0;Assembly1/1.0.0;FrameworkAssembly1/4.0.0;SomeUnknown/1.0.0""
            }
        },
        {
            ""ItemSpec"": ""tfm1/Package3/2.0.0"",
            ""Properties"": {
                ""Name"": ""Package3"",
                ""Version"": ""2.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/Assembly1/1.0.0"",
            ""Properties"": {
                ""Name"": ""Assembly1"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Assembly"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/FrameworkAssembly1/4.0.0"",
            ""Properties"": {
                ""Name"": ""FrameworkAssembly1"",
                ""Version"": ""4.0.0"",
                ""Type"": ""FrameworkAssembly"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/SomeUnknown/1.0.0"",
            ""Properties"": {
                ""Name"": ""SomeUnknown"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Xxxx"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        }
    ]
}";
            provider.LoadSnapshotFromJson(snapshotJson);

            var id = DependencyNodeId.FromString(@"file:///[MyProviderType;tfm1/Package2/1.0.0]");
            var resultNode = provider.GetDependencyNode(id);

            Assert.NotNull(resultNode);
            Assert.Equal(4, resultNode.Children.Count);

            var childrenArray = resultNode.Children.ToArray();
            Assert.True(childrenArray[0] is PackageDependencyNode);
            Assert.Equal("Package3 (2.0.0)", childrenArray[0].Caption);
            Assert.False(string.IsNullOrEmpty(childrenArray[0].Id.UniqueToken));
            Assert.True(childrenArray[1] is PackageAssemblyDependencyNode);
            Assert.Equal("Assembly1", childrenArray[1].Caption);
            Assert.False(string.IsNullOrEmpty(childrenArray[1].Id.UniqueToken));
            Assert.True(childrenArray[2] is PackageUnknownDependencyNode);
            Assert.Equal("SomeUnknown", childrenArray[2].Caption);
            Assert.False(string.IsNullOrEmpty(childrenArray[2].Id.UniqueToken));
            Assert.True(childrenArray[3] is PackageFrameworkAssembliesDependencyNode);
            Assert.False(string.IsNullOrEmpty(childrenArray[3].Id.UniqueToken));
            Assert.True(childrenArray[3].Children.First() is PackageAssemblyDependencyNode);
            Assert.Equal("FrameworkAssembly1", childrenArray[3].Children.First().Caption);
            Assert.False(string.IsNullOrEmpty(childrenArray[3].Children.First().Id.UniqueToken));

            Assert.True(provider.GetCurrentSnapshotNodesCache().Contains(id.ItemSpec));
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_ProcessDuplicatedNodes_VerifyDoesNoChanges()
        {
            var existingTopLevelNodesJson = @"
{
    ""Nodes"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/Package1/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        },
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/Package2/2.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ]  
}";
            var mockRootNode = IDependencyNodeFactory.Implement(existingTopLevelNodesJson);

            var provider = new TestableNuGetDependenciesSubTreeProvider();
            provider.SetRootNode(mockRootNode);

            var expectedDependenciesChanges = @"
{
    ""AddedNodes"": [
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/package1/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ],    
    ""UpdatedNodes"": [ ],
    ""RemovedNodes"": [ ]
}";
            var dependenciesChanges = DependenciesChangeFactory.FromJson(expectedDependenciesChanges);
            var expectedChanges = DependenciesChangeFactory.FromJson(expectedDependenciesChanges);

            // Act
            provider.TestProcessDuplicatedNodes(dependenciesChanges);
            Assert.True(DependenciesChangeFactory.AreEqual(expectedChanges, dependenciesChanges));

        }

        [Fact]
        public async Task NuGetDependenciesSubTreeProvider_SearchAsync()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var snapshotJson = @"
{
    ""NodesCache"": [],
    ""DependenciesWorld"": [
        {
            ""ItemSpec"": ""tfm1/Package2/1.0.0"",
            ""Properties"": {
                ""Name"": ""Package2"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": ""Package3/2.0.0;NotExistentPackage/2.0.0;Assembly1/1.0.0;FrameworkAssembly1/4.0.0;SomeUnknown/1.0.0""
            }
        },
        {
            ""ItemSpec"": ""tfm1/Package3/2.0.0"",
            ""Properties"": {
                ""Name"": ""Package3"",
                ""Version"": ""2.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/Assembly1/1.0.0"",
            ""Properties"": {
                ""Name"": ""Assembly1"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Assembly"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/FrameworkAssembly1/4.0.0"",
            ""Properties"": {
                ""Name"": ""FrameworkAssembly1"",
                ""Version"": ""4.0.0"",
                ""Type"": ""FrameworkAssembly"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/SomeUnknown/1.0.0"",
            ""Properties"": {
                ""Name"": ""SomeUnknown"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Xxxx"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        }
    ]
}";
            provider.LoadSnapshotFromJson(snapshotJson);

            var rootNodeNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""NuGetDependency"",
        ""ItemSpec"": ""tfm1/Package2/1.0.0"",
        ""ItemType"": ""PackageReference""
    }
}");
            var searchResults = await provider.SearchAsync(rootNodeNode, "ass");

            Assert.NotNull(searchResults);
            Assert.Equal(2, searchResults.Count());

            var searchResultsArray = searchResults.ToArray();

            Assert.True(searchResultsArray[0] is PackageAssemblyDependencyNode);
            Assert.Equal("Assembly1", searchResultsArray[0].Caption);
            Assert.False(string.IsNullOrEmpty(searchResultsArray[0].Id.UniqueToken));

            Assert.True(searchResultsArray[1] is PackageAssemblyDependencyNode);
            Assert.Equal("FrameworkAssembly1", searchResultsArray[1].Caption);
            Assert.False(string.IsNullOrEmpty(searchResultsArray[1].Id.UniqueToken));
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_DependenciesSnapshot_AddDependency()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var snapshotJson = @"
{
    ""NodesCache"": [ ],
    ""DependenciesWorld"": [
        {
            ""ItemSpec"": ""tfm1/Package2/1.0.0"",
            ""Properties"": {
                ""Name"": ""Package2"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": ""Package3/2.0.0;NotExistentPackage/2.0.0;Assembly1/1.0.0;FrameworkAssembly1/4.0.0;SomeUnknown/1.0.0""
            }
        },
        {
            ""ItemSpec"": ""tfm1/Package3/2.0.0"",
            ""Properties"": {
                ""Name"": ""Package3"",
                ""Version"": ""2.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/Assembly1/1.0.0"",
            ""Properties"": {
                ""Name"": ""Assembly1"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Assembly"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/FrameworkAssembly1/4.0.0"",
            ""Properties"": {
                ""Name"": ""FrameworkAssembly1"",
                ""Version"": ""4.0.0"",
                ""Type"": ""FrameworkAssembly"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        },
        {
            ""ItemSpec"": ""tfm1/SomeUnknown/1.0.0"",
            ""Properties"": {
                ""Name"": ""SomeUnknown"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Xxxx"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": """"
            }
        }
    ]
}";
            provider.LoadSnapshotFromJson(snapshotJson);

            // Add a dependency
            var sampleDependencyProperties = new Dictionary<string, string>
            {
                { "Name", "Package2"},
                { "Version", "3.0.0"}
            }.ToImmutableDictionary();

            provider.AddDependencyToSnapshot("tfm1/Package4/1.0.0", sampleDependencyProperties);

            var currentSnapshotDependencies = provider.GetCurrentSnapshotDependenciesWorld();

            Assert.True(currentSnapshotDependencies.Contains("tfm1/Package4/1.0.0"));

            // Add a target
            provider.AddDependencyToSnapshot("tfm1", sampleDependencyProperties);

            var currentSnapshotTargets = provider.GetCurrentSnapshotTargets();

            Assert.True(currentSnapshotTargets.Contains("tfm1"));
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_DependenciesSnapshot_UpdateDependency()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var snapshotJson = @"
{
    ""NodesCache"": [ 
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/Package2/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ],
    ""DependenciesWorld"": [
        {
            ""ItemSpec"": ""tfm1/Package2/1.0.0"",
            ""Properties"": {
                ""Name"": ""Package2"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": ""Package3/2.0.0;NotExistentPackage/2.0.0;Assembly1/1.0.0;FrameworkAssembly1/4.0.0;SomeUnknown/1.0.0""
            }
        }
    ]
}";
            provider.LoadSnapshotFromJson(snapshotJson);

            // Add a dependency
            var sampleDependencyProperties = new Dictionary<string, string>
            {
                { "Name", "Package2"},
                { "Version", "3.0.0"}
            }.ToImmutableDictionary();

            var itemsSpec = "tfm1/Package2/1.0.0";

            // Act
            provider.UpdateDependencyInSnapshot(itemsSpec, sampleDependencyProperties);

            // Assert
            // check node was updated
            var resultVersion = provider.GetCurrentSnapshotDependencyProperty(
                                                    itemsSpec, "Version");
            Assert.Equal("3.0.0", resultVersion);

            // check node was removed form cache
            Assert.False(provider.GetCurrentSnapshotNodesCache().Any(x => x.Equals(itemsSpec)));
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_DependenciesSnapshot_RemoveDependency()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            var snapshotJson = @"
{
    ""NodesCache"": [ 
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/Package2/1.0.0"",
                ""ItemType"": ""PackageReference""
            },
        },
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/PackageChild1/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        },
        {
            ""Id"": {
                ""ProviderType"": ""NuGetDependency"",
                ""ItemSpec"": ""tfm1/PackageToStayInCache/1.0.0"",
                ""ItemType"": ""PackageReference""
            }
        }
    ],
    ""DependenciesWorld"": [
        {
            ""ItemSpec"": ""tfm1/Package2/1.0.0"",
            ""Properties"": {
                ""Name"": ""Package2"",
                ""Version"": ""1.0.0"",
                ""Type"": ""Package"",
                ""Path"": ""SomePath"",
                ""Resolved"": ""true"",
                ""Dependencies"": ""Package3/2.0.0;NotExistentPackage/2.0.0;Assembly1/1.0.0;FrameworkAssembly1/4.0.0;SomeUnknown/1.0.0""
            }
        }
    ]
}";

            var childNodeInCache = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""NuGetDependency"",
        ""ItemSpec"": ""tfm1/PackageChild1/1.0.0"",
        ""ItemType"": ""PackageReference""
    }
}");

            var itemSpec = "tfm1/Package2/1.0.0";

            provider.LoadSnapshotFromJson(snapshotJson);
            provider.AddChildToNodeInCache(itemSpec, childNodeInCache);

            // Add a dependency
            var sampleDependencyProperties = new Dictionary<string, string>
            {
                { "Name", "Package2"},
                { "Version", "3.0.0"}
            }.ToImmutableDictionary();

            // Act
            provider.RemoveDependencyFromSnapshot(itemSpec);

            // Assert
            // check node and it's children were removed form cache
            var cacheNodes = provider.GetCurrentSnapshotNodesCache();

            Assert.Equal(1, cacheNodes.Count());
            Assert.Equal("tfm1/PackageToStayInCache/1.0.0", cacheNodes.First());
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_ResolvedReferenceRuleNames()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            Assert.Equal(1, provider.GetResolvedReferenceRuleNames().Count());
            Assert.Equal("ResolvedPackageReference", provider.GetResolvedReferenceRuleNames().First());
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_UnresolvedReferenceRuleNames()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            Assert.Equal(0, provider.GetUnresolvedReferenceRuleNames().Count());
        }

        [Fact]
        public void NuGetDependenciesSubTreeProvider_Icons()
        {
            var provider = new TestableNuGetDependenciesSubTreeProvider();

            Assert.Equal(5, provider.Icons.Count());

            var iconsArray = provider.Icons.ToArray();

            Assert.Equal(KnownMonikers.PackageReference, iconsArray[0]);
            Assert.Equal(KnownMonikers.Reference, iconsArray[1]);
            Assert.Equal(KnownMonikers.ReferenceWarning, iconsArray[2]);
            Assert.Equal(KnownMonikers.QuestionMark, iconsArray[3]);
            Assert.Equal(KnownMonikers.Library, iconsArray[4]);
        }

        private class TestableNuGetDependenciesSubTreeProvider : NuGetDependenciesSubTreeProvider
        {
            public TestableNuGetDependenciesSubTreeProvider()
                : base()
            {
            }

            public DependenciesChange TestDependenciesChanged(
                                            IProjectSubscriptionUpdate projectSubscriptionUpdate,
                                            IProjectCatalogSnapshot catalogs)
            {
                return ProcessDependenciesChanges(projectSubscriptionUpdate, catalogs);
            }

            public IDependencyNode TestCreateRootNode()
            {
                return CreateRootNode();
            }

            public void TestProcessDuplicatedNodes(DependenciesChange changes)
            {
                ProcessDuplicatedNodes(changes);
            }

            public ImmutableHashSet<string> GetResolvedReferenceRuleNames()
            {
                return ResolvedReferenceRuleNames;
            }

            public ImmutableHashSet<string> GetUnresolvedReferenceRuleNames()
            {
                return UnresolvedReferenceRuleNames;
            }

            public void SetRootNode(IDependencyNode node)
            {
                RootNode = node;

                if (node.Children != null)
                {
                    SetCurrentSnapshot(node.Children);
                }
            }

            public void SetCurrentSnapshot(IEnumerable<IDependencyNode> nodes)
            {
                CurrentSnapshot.DependenciesWorld.Clear();

                foreach (var node in nodes)
                {
                    CurrentSnapshot.DependenciesWorld.Add(node.Id.ItemSpec,
                                                          new DependencyMetadata(node.Id.ItemSpec,
                                                            new Dictionary<string, string>().ToImmutableDictionary()));
                }
            }

            public void SetCurrentSnapshotNodesCache(IEnumerable<IDependencyNode> nodes)
            {
                CurrentSnapshot.NodesCache.Clear();

                foreach (var node in nodes)
                {
                    CurrentSnapshot.NodesCache.Add(node.Id, node);
                }
            }

            public Dictionary<string, string> GetDependencyProperties(string itemSpec)
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                DependencyMetadata dependency = null;
                if (!CurrentSnapshot.DependenciesWorld.TryGetValue(itemSpec, out dependency))
                {
                    return result;
                }

                result.Add("Version", dependency.Version);
                result.Add("Name", dependency.Name);
                result.Add("Type", dependency.DependencyType.ToString());
                result.Add("Path", dependency.Path);
                result.Add("Dependencies", string.Join(";", dependency.DependenciesItemSpecs));
                result.Add("Resolved", dependency.Resolved.ToString());

                return result;
            }

            public List<string> GetCurrentSnapshotDependenciesWorld()
            {
                return CurrentSnapshot.DependenciesWorld.Keys.ToList();
            }

            public string GetCurrentSnapshotDependencyProperty(string itemSpec, string propertyName)
            {
                var metadata = CurrentSnapshot.DependenciesWorld[itemSpec];
                if (propertyName.Equals("Version"))
                {
                    return metadata.Version;
                }

                return null;
            }

            public List<string> GetCurrentSnapshotNodesCache()
            {
                return CurrentSnapshot.NodesCache.Keys.Select(x => x.ItemSpec).ToList();
            }

            public List<string> GetCurrentSnapshotTargets()
            {
                return CurrentSnapshot.Targets.Keys.ToList();
            }

            public void LoadSnapshotFromJson(string jsonString)
            {
                var json = JObject.Parse(jsonString);
                var data = json.ToObject<SnapshotModel>();

                if (data.NodesCache != null)
                {
                    SetCurrentSnapshotNodesCache(data.NodesCache);
                }

                if (data.DependenciesWorld != null)
                {
                    foreach(var dependency in data.DependenciesWorld)
                    {
                        CurrentSnapshot.DependenciesWorld.Add(dependency.ItemSpec,
                                                              new DependencyMetadata(dependency.ItemSpec, 
                                                                                     dependency.Properties));
                    }
                }
            }

            public void AddDependencyToSnapshot(string itemSpec, IImmutableDictionary<string, string> properties)
            {
                CurrentSnapshot.AddDependency(itemSpec, properties);
            }

            public void UpdateDependencyInSnapshot(string itemSpec, IImmutableDictionary<string, string> properties)
            {
                CurrentSnapshot.UpdateDependency(itemSpec, properties);
            }

            public void RemoveDependencyFromSnapshot(string itemSpec)
            {
                CurrentSnapshot.RemoveDependency(itemSpec);
            }

            public void AddChildToNodeInCache(string itemSpec, IDependencyNode childNode)
            {
                var node = CurrentSnapshot.NodesCache.FirstOrDefault(
                        x => x.Key.ItemSpec.Equals(itemSpec, StringComparison.OrdinalIgnoreCase)).Value;
                if (node == null)
                {
                    return;
                }

                node.AddChild(childNode);
            }

            private class SnapshotModel
            {
                public IEnumerable<DependencyNode> NodesCache { get; set; }
                public IEnumerable<DependencyMetadataModel> DependenciesWorld { get; set; }
            }

            private class DependencyMetadataModel
            {
                public string ItemSpec { get; set; }
                public IImmutableDictionary<string, string> Properties { get; set; }
            }
        }
    }
}
