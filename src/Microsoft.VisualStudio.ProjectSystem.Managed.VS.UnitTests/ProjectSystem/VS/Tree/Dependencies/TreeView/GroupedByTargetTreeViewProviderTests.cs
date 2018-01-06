// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class GroupedByTargetTreeViewProviderTests
    {
        [Fact]
        public async Task WhenEmptySnapshot_ShouldJustUpdateDependencyRootNode()
        {
            // Arrange
            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(getDependenciesRootIcon:KnownMonikers.AboutBox);
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>();
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets, hasUnresolvedDependency:false);

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
            Assert.Equal(KnownMonikers.AboutBox.ToProjectSystemType(), resultTree.Icon);
            Assert.Equal(KnownMonikers.AboutBox.ToProjectSystemType(), resultTree.ExpandedIcon);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotWithExistingDependencies_ShouldApplyChanges()
        {
            var tfm1 = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var dependencyRootXxx = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""XxxDependencyRoot"",
    ""Name"":""XxxDependencyRoot"",
    ""Caption"":""XxxDependencyRoot"",
    ""Resolved"":""true""
}");

            var dependencyXxx1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\dependency1"",
    ""Name"":""dependency1"",
    ""Path"":""dependencyXxxpath"",
    ""Caption"":""Dependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon:KnownMonikers.Uninstall, expandedIcon:KnownMonikers.Uninstall, targetFramework: tfm1);

            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot""
}");

            var dependencyYyy1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependency1"",
    ""Name"":""dependency1"",
    ""Path"":""dependencyYyypath"",
    ""Caption"":""Dependency1"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall, targetFramework: tfm1);

            var dependencyYyyExisting = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependencyExisting"",
    ""Name"":""dependencyExisting"",
    ""Path"":""dependencyExistingPath"",
    ""Caption"":""DependencyExisting"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall, targetFramework: tfm1);

            var dependencies = new List<IDependency>
            {
                dependencyXxx1,
                dependencyYyy1,
                dependencyYyyExisting
            };

            var oldRootChildToBeRemoved = new TestProjectTree
            {
                Caption = "OldRootChildToBeRemoved",
                FilePath = ""
            };

            var dependencyYyyExistingTree = new TestProjectTree
            {
                Caption = "DependencyExisting",
                FilePath = "tfm1\\yyy\\dependencyExisting"
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };
            dependencyRootYyyTree.Add(dependencyYyyExistingTree);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(oldRootChildToBeRemoved);
            dependenciesRoot.Add(dependencyRootYyyTree);

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootXxx, dependencyRootYyy });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { tfm1, dependencies }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
Caption=Dependency1, FilePath=tfm1\Yyy\dependencyYyypath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=Dependency1, FilePath=tfm1\Xxx\dependencyXxxpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsHierarchyAndIsResolved_ShouldReadd()
        {
            var tfm1 = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot"",
    ""Resolved"":""true""
}");
            var dependencyYyyExisting = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependencyExisting"",
    ""Name"":""dependencyExisting"",
    ""Path"":""dependencyExistingpath"",
    ""Caption"":""DependencyExisting"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, 
    expandedIcon: KnownMonikers.Uninstall, 
    flags: DependencyTreeFlags.SupportsHierarchy,
    targetFramework: tfm1);

            var dependencies = new List<IDependency>
            {
                dependencyYyyExisting
            };

            var dependencyYyyExistingTree = new TestProjectTree
            {
                Caption = "DependencyExisting",
                FilePath = "tfm1\\yyy\\dependencyExisting",
                CustomTag = "ShouldBeCleanedSinceNodeWillBeRecreated",
                Flags = DependencyTreeFlags.UnresolvedFlags
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };
            dependencyRootYyyTree.Add(dependencyYyyExistingTree);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(dependencyRootYyyTree);

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { tfm1, dependencies }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyExistingpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsHierarchyAndIsUnresolved_ShouldReadd()
        {
            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot"",
    ""Resolved"":""true""
}");
            var dependencyYyyExisting = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependencyExisting"",
    ""Name"":""dependencyExisting"",
    ""Caption"":""DependencyExisting"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""false""
}", icon: KnownMonikers.Uninstall,
    expandedIcon: KnownMonikers.Uninstall,
    unresolvedIcon: KnownMonikers.Uninstall,
    unresolvedExpandedIcon: KnownMonikers.Uninstall,
    flags: DependencyTreeFlags.SupportsHierarchy);

            var dependencies = new List<IDependency>
            {
                dependencyYyyExisting
            };

            var dependencyYyyExistingTree = new TestProjectTree
            {
                Caption = "DependencyExisting",
                FilePath = "tfm1\\yyy\\dependencyExisting",
                CustomTag = "ShouldBeCleanedSinceNodeWillBeRecreated",
                Flags = DependencyTreeFlags.ResolvedFlags
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };
            dependencyRootYyyTree.Add(dependencyYyyExistingTree);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(dependencyRootYyyTree);

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { ITargetFrameworkFactory.Implement(moniker: "tfm1"), dependencies }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsRule_ShouldCreateRule()
        {
            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot"",
    ""Resolved"":""true""
}");
            var dependencyYyyExisting = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependencyExisting"",
    ""Name"":""dependencyExisting"",
    ""Caption"":""DependencyExisting"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall,
    expandedIcon: KnownMonikers.Uninstall,
    flags: DependencyTreeFlags.SupportsRuleProperties);

            var dependencies = new List<IDependency>
            {
                dependencyYyyExisting
            };

            var dependencyYyyExistingTree = new TestProjectTree
            {
                Caption = "DependencyExisting",
                FilePath = "tfm1\\yyy\\dependencyExisting",
                Flags = DependencyTreeFlags.ResolvedFlags
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };
            dependencyRootYyyTree.Add(dependencyYyyExistingTree);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(dependencyRootYyyTree);

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { ITargetFrameworkFactory.Implement(moniker: "tfm1"), dependencies }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325249260, ExpandedIconHash=325249260, Rule=Yyy, IsProjectItem=False, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public async Task WheEmptySnapshotAndVisibilityMarkerProvided_ShouldDisplaySubTreeRoot()
        {
            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot"",
    ""Resolved"":""true""
}");
            var dependencyVisibilityMarker = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""someid"",
    ""Name"":""someid"",
    ""Caption"":""someid"",
    ""Resolved"":""false"",
    ""Visible"":""false""
}", flags: DependencyTreeFlags.ShowEmptyProviderRootNode);

            var dependencies = new List<IDependency>
            {
                dependencyVisibilityMarker
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(dependencyRootYyyTree);

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { ITargetFrameworkFactory.Implement(moniker: "tfm1"), dependencies }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public async Task WheEmptySnapshotAndVisibilityMarkerNotProvided_ShouldHideSubTreeRoot()
        {
            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot"",
    ""Resolved"":""true""
}");
            var dependencyVisibilityMarker = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""someid"",
    ""Name"":""someid"",
    ""Caption"":""someid"",
    ""Resolved"":""false"",
    ""Visible"":""false""
}");

            var dependencies = new List<IDependency>
            {
                dependencyVisibilityMarker
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(dependencyRootYyyTree);

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { ITargetFrameworkFactory.Implement(moniker: "tfm1"), dependencies }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public async Task WhenMultipleTargetSnapshotsWithExistingDependencies_ShouldApplyChanges()
        {
            var tfm1 = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var tfm2 = ITargetFrameworkFactory.Implement(moniker: "tfm2");
            var tfmAny = ITargetFrameworkFactory.Implement(moniker: "any");

            var dependencyRootXxx = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""XxxDependencyRoot"",
    ""Name"":""XxxDependencyRoot"",
    ""Caption"":""XxxDependencyRoot"",
    ""Resolved"":""true""
}");

            var dependencyXxx1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""xxx\\dependency1"",
    ""Path"": ""dependencyxxxpath"",
    ""Name"":""dependency1"",
    ""Caption"":""Dependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall, targetFramework: tfm1);

            var dependencyRootYyy = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""YyyDependencyRoot"",
    ""Name"":""YyyDependencyRoot"",
    ""Caption"":""YyyDependencyRoot""
}");

            var dependencyYyy1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""yyy\\dependency1"",
    ""Path"": ""dependencyyyypath"",
    ""Name"":""dependency1"",
    ""Caption"":""Dependency1"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall, targetFramework: tfm1);

            var dependencyYyyExisting = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""yyy\\dependencyExisting"",
    ""Path"": ""dependencyyyyExistingpath"",
    ""Name"":""dependencyExisting"",
    ""Caption"":""DependencyExisting"",
    ""SchemaItemType"":""Yyy"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall, targetFramework: tfm1);

            var dependencyRootZzz = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Zzz"",
    ""Id"": ""ZzzDependencyRoot"",
    ""Name"":""ZzzDependencyRoot"",
    ""Caption"":""ZzzDependencyRoot"",
    ""Resolved"":""true""
}", flags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp));
            var dependencyAny1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Zzz"",
    ""Id"": ""ZzzDependencyAny1"",
    ""Path"": ""ZzzDependencyAny1path"",
    ""Name"":""ZzzDependencyAny1"",
    ""Caption"":""ZzzDependencyAny1""
}", targetFramework:tfmAny);

            var dependencies = new List<IDependency>
            {
                dependencyXxx1,
                dependencyYyy1,
                dependencyYyyExisting
            };

            var dependenciesAny = new List<IDependency>
            {
                dependencyAny1
            };

            var oldRootChildToBeRemoved = new TestProjectTree
            {
                Caption = "OldRootChildToBeRemoved",
                FilePath = ""
            };

            var dependencyYyyExistingTree = new TestProjectTree
            {
                Caption = "DependencyExisting",
                FilePath = "yyy\\dependencyExisting"
            };

            var dependencyRootYyyTree = new TestProjectTree
            {
                Caption = "YyyDependencyRoot",
                FilePath = "YyyDependencyRoot"
            };
            dependencyRootYyyTree.Add(dependencyYyyExistingTree);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = ""
            };

            dependenciesRoot.Add(oldRootChildToBeRemoved);
            dependenciesRoot.Add(dependencyRootYyyTree);

            var target1 = IDependencyFactory.FromJson(@"
{
    ""Id"": ""tfm1"",
    ""Name"":""tfm1"",
    ""Caption"":""tfm1""
}");
            var target2 = IDependencyFactory.FromJson(@"
{
    ""Id"": ""tfm2"",
    ""Name"":""tfm2"",
    ""Caption"":""tfm2""
}");
            var targetAny = IDependencyFactory.FromJson(@"
{
    ""Id"": ""any"",
    ""Name"":""any"",
    ""Caption"":""any""
}");

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootXxx, dependencyRootYyy, dependencyRootZzz },
                createTargetViewModel: new[] { target1 , target2 });

            var testData = new Dictionary<ITargetFramework, List<IDependency>>
            {
                { tfm1, dependencies },
                { tfm2, dependencies },
                { tfmAny, dependenciesAny }
            };

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefodler\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, GetSnapshot(testData));

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
Caption=ZzzDependencyRoot, FilePath=ZzzDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=ZzzDependencyAny1, FilePath=ZzzDependencyAny1, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=tfm2, FilePath=tfm2, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyyyyExistingpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
Caption=Dependency1, FilePath=tfm1\Yyy\dependencyyyypath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=Dependency1, FilePath=tfm1\Xxx\dependencyxxxpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
Caption=tfm1, FilePath=tfm1, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyyyyExistingpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
Caption=Dependency1, FilePath=tfm1\Yyy\dependencyyyypath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
Caption=Dependency1, FilePath=tfm1\Xxx\dependencyxxxpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
";
            Assert.Equal(expectedFlatHierarchy, ((TestProjectTree)resultTree).FlatHierarchy);
        }

        [Fact]
        public void WhenFindByPathAndNoolNode_ShouldDoNothing()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = "",
                Flags = ProjectTreeFlags.Empty
            };

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(null, Path.Combine(projectFolder, @"somenode"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndNotDependenciesRoot_ShouldDoNothing()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = "",
                Flags = ProjectTreeFlags.Empty
            };

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(dependenciesRoot, Path.Combine(projectFolder, @"somenode"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndAbsoluteNodePath_ShouldFind()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = "",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags
            };

            var level1Child1 = new TestProjectTree
            {
                Caption = "level1Child1",
                FilePath = @"c:\folder\level1Child1",
                Flags = ProjectTreeFlags.Empty
            };

            var level1Child2 = new TestProjectTree
            {
                Caption = "level1Child2",
                FilePath = @"c:\folder\level1Child2",
                Flags = ProjectTreeFlags.Empty
            };

            var level2Child21 = new TestProjectTree
            {
                Caption = "level2Child21",
                FilePath = @"c:\folder\level2Child21",
                Flags = ProjectTreeFlags.Empty
            };

            var level2Child22 = new TestProjectTree
            {
                Caption = "level1Child22",
                FilePath = @"c:\folder\level2Child22",
                Flags = ProjectTreeFlags.Empty
            };

            var level3Child31 = new TestProjectTree
            {
                Caption = "level3Child31",
                FilePath = @"c:\folder\level3Child31",
                Flags = ProjectTreeFlags.Empty
            };

            var level3Child32 = new TestProjectTree
            {
                Caption = "level3Child32",
                FilePath = @"c:\folder\level3Child32",
                Flags = ProjectTreeFlags.Empty
            };

            dependenciesRoot.Add(level1Child1);
            dependenciesRoot.Add(level1Child2);

            level1Child2.Add(level2Child21);
            level1Child2.Add(level2Child22);

            level2Child22.Add(level3Child31);
            level2Child22.Add(level3Child32);

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(dependenciesRoot, @"c:\folder\level3Child32");

            // Assert
            Assert.NotNull(resultTree);
            Assert.Equal("level3Child32", resultTree.Caption);
        }

        [Fact]
        public void WhenFindByPathAndRelativeNodePath_ShouldNotFind()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create(filePath:projectPath);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = "",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags
            };

            var level1Child1 = new TestProjectTree
            {
                Caption = "level1Child1",
                FilePath = @"c:\folder\level1Child1",
                Flags = ProjectTreeFlags.Empty
            };

            var level1Child2 = new TestProjectTree
            {
                Caption = "level1Child2",
                FilePath = @"c:\folder\level1Child2",
                Flags = ProjectTreeFlags.Empty
            };

            var level2Child21 = new TestProjectTree
            {
                Caption = "level2Child21",
                FilePath = @"c:\folder\level2Child21",
                Flags = ProjectTreeFlags.Empty
            };

            var level2Child22 = new TestProjectTree
            {
                Caption = "level1Child22",
                FilePath = @"c:\folder\level2Child22",
                Flags = ProjectTreeFlags.Empty
            };

            var level3Child31 = new TestProjectTree
            {
                Caption = "level3Child31",
                FilePath = @"c:\folder\level3Child31",
                Flags = ProjectTreeFlags.Empty
            };

            var level3Child32 = new TestProjectTree
            {
                Caption = "level3Child32",
                FilePath = @"level3Child32",
                Flags = ProjectTreeFlags.Empty
            };

            dependenciesRoot.Add(level1Child1);
            dependenciesRoot.Add(level1Child2);

            level1Child2.Add(level2Child21);
            level1Child2.Add(level2Child22);

            level2Child22.Add(level3Child31);
            level2Child22.Add(level3Child32);

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(dependenciesRoot, Path.Combine(projectFolder, @"level3Child32"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndNeedToFindDependenciesRoot_ShouldNotFind()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create(filePath: projectPath);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var projectRoot = new TestProjectTree
            {
                Caption = "myproject",
                FilePath = "",
                Flags = ProjectTreeFlags.Empty
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                FilePath = "",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags
            };

            var level1Child1 = new TestProjectTree
            {
                Caption = "level1Child1",
                FilePath = @"c:\folder\level1Child1",
                Flags = ProjectTreeFlags.Empty
            };

            var level1Child2 = new TestProjectTree
            {
                Caption = "level1Child2",
                FilePath = @"c:\folder\level1Child2",
                Flags = ProjectTreeFlags.Empty
            };

            var level2Child21 = new TestProjectTree
            {
                Caption = "level2Child21",
                FilePath = @"c:\folder\level2Child21",
                Flags = ProjectTreeFlags.Empty
            };

            var level2Child22 = new TestProjectTree
            {
                Caption = "level1Child22",
                FilePath = @"c:\folder\level2Child22",
                Flags = ProjectTreeFlags.Empty
            };

            var level3Child31 = new TestProjectTree
            {
                Caption = "level3Child31",
                FilePath = @"c:\folder\level3Child31",
                Flags = ProjectTreeFlags.Empty
            };

            var level3Child32 = new TestProjectTree
            {
                Caption = "level3Child32",
                FilePath = @"level3Child32",
                Flags = ProjectTreeFlags.Empty
            };

            projectRoot.Add(dependenciesRoot);

            dependenciesRoot.Add(level1Child1);
            dependenciesRoot.Add(level1Child2);

            level1Child2.Add(level2Child21);
            level1Child2.Add(level2Child22);

            level2Child22.Add(level3Child31);
            level2Child22.Add(level3Child32);

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);

            var result = provider.FindByPath(projectRoot, Path.Combine(projectFolder, @"level3Child32"));

            // Assert
            Assert.Null(result);
        }

        private IDependenciesSnapshot GetSnapshot(Dictionary<ITargetFramework, List<IDependency>> testData)
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>();
            foreach (var kvp in testData)
            {
                var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                    catalogs: catalogs,
                    topLevelDependencies: kvp.Value,
                    checkForUnresolvedDependencies: false,
                    targetFramework: kvp.Key);

                targets.Add(kvp.Key, targetedSnapshot);
            }
            return IDependenciesSnapshotFactory.Implement(
                targets: targets,
                hasUnresolvedDependency: false,
                activeTarget: testData.First().Key);
        }
    }
}
