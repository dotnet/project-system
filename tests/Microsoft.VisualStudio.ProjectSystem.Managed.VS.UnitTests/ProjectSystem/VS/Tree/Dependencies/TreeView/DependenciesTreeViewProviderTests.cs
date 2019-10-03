// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.TreeView
{
    public sealed class DependenciesTreeViewProviderTests
    {
        private const string ProjectPath = @"c:\myfolder\mysubfolder\myproject.csproj";

        private readonly ITargetFramework _tfm1 = new TargetFramework("tfm1");
        private readonly ITargetFramework _tfm2 = new TargetFramework("tfm2");

        private static readonly ImageMoniker s_rootImage = KnownMonikers.AboutBox;

        [Fact]
        public async Task BuildTreeAsync_EmptySnapshot_CreatesRootNode()
        {
            // Arrange
            var dependenciesRoot = new TestProjectTree { Caption = "MyDependencies" };

            var snapshot = DependenciesSnapshot.CreateEmpty(ProjectPath);

            // Act
            var resultTree = await CreateProvider().BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy = "Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
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
                Name = "XxxDependencyRoot",
                Caption = "XxxDependencyRoot",
                Resolved = true
            };

            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot"
            };

            var dependencyXxx1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "dependency1",
                Path = "dependencyXxxpath",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TargetFramework = _tfm1
            };

            var dependencyYyy1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependency1",
                Name = "dependency1",
                Path = "dependencyYyypath",
                Caption = "Dependency1",
                SchemaItemType = "Yyy",
                Resolved = true,
                TargetFramework = _tfm1
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Path = "dependencyExistingPath",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                TargetFramework = _tfm1
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
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting"
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
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
        Caption=Dependency1, FilePath=tfm1\Yyy\dependencyYyypath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
    Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=Dependency1, FilePath=tfm1\Xxx\dependencyXxxpath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsHierarchyAndIsResolved_ShouldRead()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Path = "dependencyExistingpath",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                Flags = DependencyTreeFlags.SupportsHierarchy,
                TargetFramework = _tfm1
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting",
                                CustomTag = "ShouldBeCleanedSinceNodeWillBeRecreated",
                                Flags = DependencyTreeFlags.Unresolved
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
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyExistingpath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsHierarchyAndIsUnresolved_ShouldRead()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = false,
                Flags = DependencyTreeFlags.SupportsHierarchy
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting",
                                CustomTag = "ShouldBeCleanedSinceNodeWillBeRecreated",
                                Flags = DependencyTreeFlags.Resolved
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
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325248665, ExpandedIconHash=325248817, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsRule_ShouldCreateRule()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
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
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting",
                                Flags = DependencyTreeFlags.Resolved
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
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325248088, ExpandedIconHash=325248260, Rule=Yyy, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenEmptySnapshotAndVisibilityMarkerProvided_ShouldDisplaySubTreeRoot()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyVisibilityMarker = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "someid",
                Name = "someid",
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
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot"
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyVisibilityMarker }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenEmptySnapshotAndVisibilityMarkerNotProvided_ShouldHideSubTreeRoot()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyVisibilityMarker = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "someid",
                Name = "someid",
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
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot"
                    }
                }
            };

            var provider = CreateProvider(rootModels: new[] { dependencyModelRootYyy });

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyVisibilityMarker }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenMultipleTargetSnapshotsWithExistingDependencies_ShouldApplyChanges()
        {
            var dependencyModelRootXxx = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "XxxDependencyRoot",
                Name = "XxxDependencyRoot",
                Caption = "XxxDependencyRoot",
                Resolved = true
            };

            var dependencyXxx1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "xxx\\dependency1",
                Path = "dependencyxxxpath",
                Name = "dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TargetFramework = _tfm1
            };

            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot"
            };

            var dependencyYyy1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "yyy\\dependency1",
                Path = "dependencyyyypath",
                Name = "dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Yyy",
                Resolved = true,
                TargetFramework = _tfm1
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "yyy\\dependencyExisting",
                Path = "dependencyyyyExistingpath",
                Name = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                TargetFramework = _tfm1
            };

            var dependencyModelRootZzz = new TestDependencyModel
            {
                ProviderType = "Zzz",
                Id = "ZzzDependencyRoot",
                Name = "ZzzDependencyRoot",
                Caption = "ZzzDependencyRoot",
                Resolved = true,
                Flags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp)
            };

            var dependencyAny1 = new TestDependency
            {
                ProviderType = "Zzz",
                Id = "ZzzDependencyAny1",
                Path = "ZzzDependencyAny1path",
                Name = "ZzzDependencyAny1",
                Caption = "ZzzDependencyAny1",
                TargetFramework = TargetFramework.Any
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
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "yyy\\dependencyExisting"
                            }
                        }
                    }
                }
            };

            var targetModel1 = new TestDependencyModel
            {
                Id = "tfm1",
                Name = "tfm1",
                Caption = "tfm1"
            };

            var targetModel2 = new TestDependencyModel
            {
                Id = "tfm2",
                Name = "tfm2",
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
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=ZzzDependencyRoot, FilePath=ZzzDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=ZzzDependencyAny1, FilePath=ZzzDependencyAny1, IconHash=325248665, ExpandedIconHash=325248817, Rule=, IsProjectItem=False, CustomTag=
    Caption=tfm2, FilePath=tfm2, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
        Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyyyyExistingpath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Yyy\dependencyyyypath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
        Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Xxx\dependencyxxxpath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=False, CustomTag=
    Caption=tfm1, FilePath=tfm1, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
        Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyyyyExistingpath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Yyy\dependencyyyypath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=
        Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Xxx\dependencyxxxpath, IconHash=325248088, ExpandedIconHash=325248260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public void WhenFindByPathAndNullNode_ShouldDoNothing()
        {
            // Arrange
            var projectFolder = Path.GetDirectoryName(ProjectPath);
            var provider = CreateProvider();

            // Act
            var resultTree = provider.FindByPath(null, Path.Combine(projectFolder, @"somenode"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndNotDependenciesRoot_ShouldDoNothing()
        {
            // Arrange
            var projectFolder = Path.GetDirectoryName(ProjectPath);
            var provider = CreateProvider();
            var dependenciesRoot = new TestProjectTree { Caption = "MyDependencies" };

            // Act
            var resultTree = provider.FindByPath(dependenciesRoot, Path.Combine(projectFolder, @"somenode"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndAbsoluteNodePath_ShouldFind()
        {
            // Arrange
            var provider = CreateProvider();

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags,
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "level1Child1",
                        FilePath = @"c:\folder\level1Child1"
                    },
                    new TestProjectTree
                    {
                        Caption = "level1Child2",
                        FilePath = @"c:\folder\level1Child2",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "level2Child21",
                                FilePath = @"c:\folder\level2Child21"
                            },
                            new TestProjectTree
                            {
                                Caption = "level1Child22",
                                FilePath = @"c:\folder\level2Child22",
                                Children =
                                {
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child31",
                                        FilePath = @"c:\folder\level3Child31"
                                    },
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child32",
                                        FilePath = @"c:\folder\level3Child32"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var resultTree = provider.FindByPath(dependenciesRoot, @"c:\folder\level3Child32");

            // Assert
            Assert.NotNull(resultTree);
            Assert.Equal("level3Child32", resultTree!.Caption);
        }

        [Fact]
        public void WhenFindByPathAndRelativeNodePath_ShouldNotFind()
        {
            // Arrange
            var projectFolder = Path.GetDirectoryName(ProjectPath);

            var provider = CreateProvider();

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags,
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "level1Child1",
                        FilePath = @"c:\folder\level1Child1"
                    },
                    new TestProjectTree
                    {
                        Caption = "level1Child2",
                        FilePath = @"c:\folder\level1Child2",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "level2Child21",
                                FilePath = @"c:\folder\level2Child21"
                            },
                            new TestProjectTree
                            {
                                Caption = "level1Child22",
                                FilePath = @"c:\folder\level2Child22",
                                Children =
                                {
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child31",
                                        FilePath = @"c:\folder\level3Child31"
                                    },
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child32",
                                        FilePath = @"level3Child32"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var resultTree = provider.FindByPath(dependenciesRoot, Path.Combine(projectFolder, @"level3Child32"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndNeedToFindDependenciesRoot_ShouldNotFind()
        {
            // Arrange
            var projectFolder = Path.GetDirectoryName(ProjectPath);

            var provider = CreateProvider();

            var projectRoot = new TestProjectTree
            {
                Caption = "myproject",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "MyDependencies",
                        Flags = DependencyTreeFlags.DependenciesRootNodeFlags,
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "level1Child1",
                                FilePath = @"c:\folder\level1Child1"
                            },
                            new TestProjectTree
                            {
                                Caption = "level1Child2",
                                FilePath = @"c:\folder\level1Child2",
                                Children =
                                {
                                    new TestProjectTree
                                    {
                                        Caption = "level2Child21",
                                        FilePath = @"c:\folder\level2Child21"
                                    },
                                    new TestProjectTree
                                    {
                                        Caption = "level1Child22",
                                        FilePath = @"c:\folder\level2Child22",
                                        Children =
                                        {
                                            new TestProjectTree
                                            {
                                                Caption = "level3Child31",
                                                FilePath = @"c:\folder\level3Child31"
                                            },
                                            new TestProjectTree
                                            {
                                                Caption = "level3Child32",
                                                FilePath = @"level3Child32"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var result = provider.FindByPath(projectRoot, Path.Combine(projectFolder, @"level3Child32"));

            // Assert
            Assert.Null(result);
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
                project: UnconfiguredProjectFactory.Create(filePath: ProjectPath));

            return new DependenciesTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
        }

        private static DependenciesSnapshot GetSnapshot(params (ITargetFramework tfm, IReadOnlyList<IDependency> dependencies)[] testData)
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var dependenciesByTarget = new Dictionary<ITargetFramework, TargetedDependenciesSnapshot>();

            foreach ((ITargetFramework tfm, IReadOnlyList<IDependency> dependencies) in testData)
            {
                var targetedSnapshot = new TargetedDependenciesSnapshot(
                    "ProjectPath",
                    tfm,
                    catalogs,
                    dependencies.ToImmutableDictionary(d => d.Id).WithComparers(StringComparer.OrdinalIgnoreCase));

                dependenciesByTarget.Add(tfm, targetedSnapshot);
            }

            return new DependenciesSnapshot(
                ProjectPath,
                testData[0].tfm,
                dependenciesByTarget.ToImmutableDictionary());
        }

        private static string ToTestDataString(TestProjectTree root)
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
                    builder.Append("FilePath=").Append(tree.FilePath).Append(", ");
                    builder.Append("IconHash=").Append(tree.Icon.GetHashCode()).Append(", ");
                    builder.Append("ExpandedIconHash=").Append(tree.ExpandedIcon.GetHashCode()).Append(", ");
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
