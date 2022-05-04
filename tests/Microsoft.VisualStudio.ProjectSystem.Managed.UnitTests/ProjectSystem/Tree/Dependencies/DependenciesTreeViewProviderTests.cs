// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    public sealed class DependenciesTreeViewProviderTests
    {
        private readonly TargetFramework _tfm1 = new("tfm1");
        private readonly TargetFramework _tfm2 = new("tfm2");

        private readonly ITestOutputHelper _output;

        private static readonly ImageMoniker s_rootImage = KnownMonikers.AboutBox;

        public DependenciesTreeViewProviderTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task BuildTreeAsync_EmptySnapshot_CreatesRootNode()
        {
            // Arrange
            var dependenciesRoot = new TestProjectTree { Caption = "MyDependencies" };

            var snapshot = DependenciesSnapshot.Empty;

            // Act
            var resultTree = await CreateProvider().BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy = "Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=";
            AssertTestData(expectedFlatHierarchy, resultTree);
            Assert.Equal(s_rootImage.ToProjectSystemType(), resultTree.Icon);
            Assert.Equal(s_rootImage.ToProjectSystemType(), resultTree.ExpandedIcon);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotWithExistingDependencies_ShouldApplyChanges()
        {
            var dependencyModelRootXxx = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "XxxDependencyRoot",
                OriginalItemSpec = "XxxDependencyRoot",
                Caption = "XxxDependencyRoot",
                Resolved = true
            };

            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot"
            };

            var dependencyXxx1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                OriginalItemSpec = "dependency1",
                FilePath = "dependencyXxxpath",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyYyy1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependency1",
                OriginalItemSpec = "dependency1",
                FilePath = "dependencyYyypath",
                Caption = "Dependency1",
                SchemaItemType = "Yyy",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependencyExisting",
                OriginalItemSpec = "dependencyExisting",
                FilePath = "dependencyExistingPath",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "OldRootChildToBeRemoved"
                    },
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting"
                            }
                        }
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] {dependencyModelRootXxx, dependencyModelRootYyy});

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyXxx1, dependencyYyy1, dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            const string expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                    Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                        Caption=DependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
                        Caption=Dependency1, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
                    Caption=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                        Caption=Dependency1, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencyIsResolved_ShouldRead()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependencyExisting",
                OriginalItemSpec = "dependencyExisting",
                FilePath = "dependencyExistingpath",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                CustomTag = "Untouched",
                                Flags = ProjectTreeFlags.BrokenReference
                            }
                        }
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                    Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                        Caption=DependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=Untouched
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencyIsUnresolved_ShouldRead()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependencyExisting",
                OriginalItemSpec = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = false
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                CustomTag = "Untouched",
                                Flags = ProjectTreeFlags.ResolvedReference
                            }
                        }
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                    Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                        Caption=DependencyExisting, IconHash=325248665, ExpandedIconHash=325248817, Rule=, IsProjectItem=False, CustomTag=Untouched
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsRule_ShouldCreateRule()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependencyExisting",
                OriginalItemSpec = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                Flags = DependencyTreeFlags.SupportsRuleProperties
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                Flags = ProjectTreeFlags.ResolvedReference
                            }
                        }
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                    Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                        Caption=DependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=Yyy, IsProjectItem=False, CustomTag=
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        [Fact]
        public async Task WhenEmptySnapshotAndVisibilityMarkerProvided_ShouldDisplaySubTreeRoot()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyVisibilityMarker = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "someid",
                OriginalItemSpec = "someid",
                Caption = "someid",
                Resolved = false,
                Visible = false,
                Flags = DependencyTreeFlags.ShowEmptyProviderRootNode
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot"
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyVisibilityMarker }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                    Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        [Fact]
        public async Task WhenEmptySnapshotAndVisibilityMarkerNotProvided_ShouldHideSubTreeRoot()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyVisibilityMarker = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "someid",
                OriginalItemSpec = "someid",
                Caption = "someid",
                Resolved = false,
                Visible = false
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot"
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyVisibilityMarker }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        [Fact]
        public async Task WhenMultipleTargetSnapshotsWithExistingDependencies_ShouldApplyChanges()
        {
            var dependencyModelRootXxx = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "XxxDependencyRoot",
                OriginalItemSpec = "XxxDependencyRoot",
                Caption = "XxxDependencyRoot",
                Resolved = true
            };

            var dependencyXxx1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                FilePath = "dependencyxxxpath",
                OriginalItemSpec = "dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                OriginalItemSpec = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot"
            };

            var dependencyYyy1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependency1",
                FilePath = "dependencyyyypath",
                OriginalItemSpec = "dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Yyy",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "dependencyExisting",
                FilePath = "dependencyyyyExistingpath",
                OriginalItemSpec = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true
            };

            var dependencyModelRootZzz = new TestDependencyModel
            {
                ProviderType = "Zzz",
                Id = "ZzzDependencyRoot",
                OriginalItemSpec = "ZzzDependencyRoot",
                Caption = "ZzzDependencyRoot",
                Resolved = true,
                Flags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp)
            };

            var dependencyAny1 = new TestDependency
            {
                ProviderType = "Zzz",
                Id = "ZzzDependencyAny1",
                FilePath = "ZzzDependencyAny1path",
                OriginalItemSpec = "ZzzDependencyAny1",
                Caption = "ZzzDependencyAny1"
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "OldRootChildToBeRemoved"
                    },
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                            }
                        }
                    }
                }
            };

            var targetModel1 = new TestDependencyModel
            {
                Id = "tfm1",
                OriginalItemSpec = "tfm1",
                Caption = "tfm1"
            };

            var targetModel2 = new TestDependencyModel
            {
                Id = "tfm2",
                OriginalItemSpec = "tfm2",
                Caption = "tfm2"
            };

            var provider = CreateProvider(
                rootModels: new[] { dependencyModelRootXxx, dependencyModelRootYyy, dependencyModelRootZzz },
                targetModels: new[] { targetModel1, targetModel2 });

            var snapshot = GetSnapshot(
                (_tfm1, new[] { dependencyXxx1, dependencyYyy1, dependencyYyyExisting }),
                (_tfm2, new[] { dependencyXxx1, dependencyYyy1, dependencyYyyExisting }),
                (TargetFramework.Any, new[] { dependencyAny1 }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            const string expectedFlatHierarchy =
                """
                Caption=MyDependencies, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
                    Caption=ZzzDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                        Caption=ZzzDependencyAny1, IconHash=325248665, ExpandedIconHash=325248817, Rule=, IsProjectItem=False, CustomTag=
                    Caption=tfm2, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
                        Caption=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                            Caption=Dependency1, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
                        Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                            Caption=Dependency1, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
                            Caption=DependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
                    Caption=tfm1, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
                        Caption=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                            Caption=Dependency1, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
                        Caption=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
                            Caption=Dependency1, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
                            Caption=DependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
                """;
            AssertTestData(expectedFlatHierarchy, resultTree);
        }

        private static DependenciesTreeViewProvider CreateProvider(
            IEnumerable<IDependencyModel>? rootModels = null,
            IEnumerable<IDependencyModel>? targetModels = null)
        {
            var treeServices = new MockIDependenciesTreeServices();

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: s_rootImage,
                createRootViewModel: rootModels,
                createTargetViewModel: targetModels);

            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(
                project: UnconfiguredProjectFactory.Create(fullPath: @"c:\Project\Project.csproj"));

            return new DependenciesTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
        }

        private static DependenciesSnapshot GetSnapshot(params (TargetFramework tfm, IReadOnlyList<IDependency> dependencies)[] testData)
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var dependenciesByTarget = new Dictionary<TargetFramework, TargetedDependenciesSnapshot>();

            foreach ((TargetFramework tfm, IReadOnlyList<IDependency> dependencies) in testData)
            {
                var targetedSnapshot = new TargetedDependenciesSnapshot(
                    tfm,
                    catalogs,
                    dependencies.ToImmutableArray());

                dependenciesByTarget.Add(tfm, targetedSnapshot);
            }

            return new DependenciesSnapshot(
                testData[0].tfm,
                dependenciesByTarget.ToImmutableDictionary());
        }

        private void AssertTestData(string expected, IProjectTree? resultTree)
        {
            Assert.NotNull(resultTree);

            string actual = ToTestDataString((TestProjectTree)resultTree);

            if (expected != actual)
            {
                _output.WriteLine("EXPECTED");
                _output.WriteLine(expected);
                _output.WriteLine("ACTUAL");
                _output.WriteLine(actual);
            }

            Assert.Equal(expected, actual);

            return;

            static string ToTestDataString(TestProjectTree root)
            {
                var builder = new StringBuilder();

                GetChildrenTestStats(root, indent: 0);

                return builder.ToString();

                void GetChildrenTestStats(TestProjectTree tree, int indent)
                {
                    WriteLine();

                    foreach (var child in tree.Children)
                    {
                        builder.AppendLine();
                        GetChildrenTestStats(child, indent + 1);
                    }

                    void WriteLine()
                    {
                        builder.Append(' ', indent * 4);
                        builder.Append("Caption=").Append(tree.Caption).Append(", ");
                        builder.Append("IconHash=").Append(tree.Icon?.GetHashCode()).Append(", ");
                        builder.Append("ExpandedIconHash=").Append(tree.ExpandedIcon?.GetHashCode()).Append(", ");
                        builder.Append("Rule=").Append(tree.BrowseObjectProperties?.Name ?? "").Append(", ");
                        builder.Append("IsProjectItem=").Append(tree.IsProjectItem).Append(", ");
                        builder.Append("CustomTag=").Append(tree.CustomTag);

                        if (tree.Flags.Contains(ProjectTreeFlags.Common.BubbleUp))
                        {
                            builder.Append(", BubbleUpFlag=True");
                        }
                    }
                }
            }
        }
    }
}
