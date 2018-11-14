// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class TargetedDependenciesSnapshotTests
    {
        [Fact]
        public void TConstructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("projectPath", () => new TargetedDependenciesSnapshot(null, null, null, null, null));
            Assert.Throws<ArgumentNullException>("targetFramework", () => new TargetedDependenciesSnapshot("path", null, null, null, null));
            Assert.Throws<ArgumentNullException>("topLevelDependencies", () => new TargetedDependenciesSnapshot("path", TargetFramework.Any, null, null, null));
            Assert.Throws<ArgumentNullException>("dependenciesWorld", () => new TargetedDependenciesSnapshot("path", TargetFramework.Any, null, ImmutableHashSet<IDependency>.Empty, null));
        }

        [Fact]
        public void TConstructor()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableHashSet<IDependency>.Empty, 
                ImmutableDictionary<string, IDependency>.Empty);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.FullName);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.Empty(snapshot.TopLevelDependencies);
            Assert.Empty(snapshot.DependenciesWorld);
        }

        [Fact]
        public void TFromChanges_Empty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: Array.Empty<IDependencyModel>(), 
                removedNodes: Array.Empty<RemovedDependencyIdentity>());

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                Array.Empty<IDependenciesSnapshotFilter>(),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void TFromChanges_NoChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyChild1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\childdependency1"",
                    ""Name"":""ChildDependency1"",
                    ""Caption"":""ChildDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyChild1.Id, dependencyChild1 },
                },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: Array.Empty<IDependencyModel>(), 
                removedNodes: Array.Empty<RemovedDependencyIdentity>());

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                Array.Empty<IDependenciesSnapshotFilter>(),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void TFromChanges_NoChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyChild1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\childdependency1"",
                    ""Name"":""ChildDependency1"",
                    ""Caption"":""ChildDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyChild1.Id, dependencyChild1 },
                },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: Array.Empty<IDependencyModel>(), 
                removedNodes: new [] { new RemovedDependencyIdentity(dependencyTop1.ProviderType, dependencyTop1.Id ) });

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeRemoveResult(FilterAction.Cancel, @"tfm1\xxx\newdependency1", null);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void TFromChanges_ReportedChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyChild1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\childdependency1"",
                    ""Name"":""ChildDependency1"",
                    ""Caption"":""ChildDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyChild1.Id, dependencyChild1 },
                },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: Array.Empty<IDependencyModel>(), 
                removedNodes: new [] { new RemovedDependencyIdentity("Xxx", "topdependency1") });

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeRemoveResult(FilterAction.Cancel, @"tfm1\xxx\topdependency1", null)
                    .ImplementFilterAnyChanges(true);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.Single(snapshot.TopLevelDependencies);
            AssertEx.CollectionLength(snapshot.DependenciesWorld, 2);
        }

        [Fact]
        public void TFromChanges_NoChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyChild1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\childdependency1"",
                    ""Name"":""ChildDependency1"",
                    ""Caption"":""ChildDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelNew1 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""newdependency1"",
                    ""Name"":""NewDependency1"",
                    ""Caption"":""NewDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyChild1.Id, dependencyChild1 },
                },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: new [] { dependencyModelNew1 }, 
                removedNodes: Array.Empty<RemovedDependencyIdentity>());

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeAddResult(FilterAction.Cancel, @"tfm1\xxx\newdependency1", null);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void TFromChanges_ReportedChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyChild1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\childdependency1"",
                    ""Name"":""ChildDependency1"",
                    ""Caption"":""ChildDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelNew1 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""newdependency1"",
                    ""Name"":""NewDependency1"",
                    ""Caption"":""NewDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyChild1.Id, dependencyChild1 },
                },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: new [] { dependencyModelNew1 }, 
                removedNodes: Array.Empty<RemovedDependencyIdentity>());

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeAddResult(FilterAction.Cancel, @"tfm1\xxx\newdependency1", null)
                    .ImplementFilterAnyChanges(true);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.Single(snapshot.TopLevelDependencies);
            AssertEx.CollectionLength(snapshot.DependenciesWorld, 2);
        }

        [Fact]
        public void TFromChanges_RemovedAndAddedChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyChild1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""childdependency1"",
                    ""Name"":""ChildDependency1"",
                    ""Caption"":""ChildDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelAdded1 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""addeddependency1"",
                    ""Name"":""AddedDependency1"",
                    ""Caption"":""AddedDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelAdded2 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""addeddependency2"",
                    ""Name"":""AddedDependency2"",
                    ""Caption"":""AddedDependency2"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelAdded3 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""addeddependency3"",
                    ""Name"":""AddedDependency3"",
                    ""Caption"":""AddedDependency3"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyAdded2Changed = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\addeddependency2"",
                    ""Name"":""AddedDependency2Changed"",
                    ""Caption"":""AddedDependency2Changed"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyRemoved1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\Removeddependency1"",
                    ""Name"":""RemovedDependency1"",
                    ""Caption"":""RemovedDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyInsteadRemoved1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\InsteadRemoveddependency1"",
                    ""Name"":""InsteadRemovedDependency1"",
                    ""Caption"":""InsteadRemovedDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            Assert.True(dependencyTop1.TopLevel);
            Assert.False(dependencyChild1.TopLevel);
            Assert.False(dependencyRemoved1.TopLevel);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyChild1.Id, dependencyChild1 },
                    { dependencyRemoved1.Id, dependencyRemoved1 },
                },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: new [] { dependencyModelAdded1, dependencyModelAdded2, dependencyModelAdded3 },
                removedNodes: new[] { new RemovedDependencyIdentity("Xxx", "Removeddependency1") });

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .ImplementBeforeAddResult(FilterAction.Cancel, @"tfm1\xxx\addeddependency1", null)
                .ImplementBeforeAddResult(FilterAction.ShouldBeAdded, @"tfm1\xxx\addeddependency2", dependencyAdded2Changed)
                .ImplementBeforeRemoveResult(FilterAction.ShouldBeAdded, @"tfm1\xxx\Removeddependency1", dependencyInsteadRemoved1);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            AssertEx.CollectionLength(snapshot.TopLevelDependencies, 2);
            Assert.Contains(snapshot.TopLevelDependencies, x => x.Id.Equals(@"topdependency1"));
            Assert.Contains(snapshot.TopLevelDependencies, x => x.Id.Equals(@"tfm1\xxx\addeddependency2") && x.Caption.Equals("AddedDependency2Changed"));
            AssertEx.CollectionLength(snapshot.DependenciesWorld, 5);
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\topdependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\childdependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\addeddependency2"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\InsteadRemoveddependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\addeddependency3"));
        }

        [Fact]
        public void TFromChanges_UpdatesTopLevelDependencies()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyTopPrevious = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"": ""TopDependency1"",
                    ""Caption"": ""TopDependency1"",
                    ""SchemaItemType"": ""Xxx"",
                    ""Resolved"": ""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelTopAdded = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""topdependency1"",
                    ""Name"": ""TopDependency1"",
                    ""Caption"": ""TopDependency1"",
                    ""SchemaItemType"": ""Xxx"",
                    ""Resolved"": ""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyTopUpdated = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"": ""TopDependency1"",
                    ""Caption"": ""TopDependency1"",
                    ""SchemaItemType"": ""Xxx"",
                    ""Resolved"": ""true""
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase),
                topLevelDependencies: new[] { dependencyTopPrevious });

            var changes = IDependenciesChangesFactory.Implement(
                addedNodes: new[] { dependencyModelTopAdded },
                removedNodes: Array.Empty<RemovedDependencyIdentity>());

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeAddResult(FilterAction.ShouldBeAdded, @"tfm1\xxx\topdependency1", dependencyTopUpdated);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(dependencyTopUpdated, snapshot.TopLevelDependencies.Single());
        }

        /// <summary>
        /// Added because circular dependencies can cause stack overflows
        /// https://github.com/dotnet/project-system/issues/3374
        /// </summary>
        [Fact]
        public void TCheckForUnresolvedDependencies_CircularDependency_DoesNotRecurseInfinitely()
        {
            var dependencyTop1 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency1"",
                    ""Name"":""TopDependency1"",
                    ""Caption"":""TopDependency1"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""true"",
                    ""DependencyIDs"": [ ""tfm1\\xxx\\topdependency2"" ]
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var dependencyTop2 = IDependencyFactory.FromJson(@"
                {
                    ""ProviderType"": ""Xxx"",
                    ""Id"": ""tfm1\\xxx\\topdependency2"",
                    ""Name"":""TopDependency2"",
                    ""Caption"":""TopDependency2"",
                    ""SchemaItemType"":""Xxx"",
                    ""Resolved"":""true"",
                    ""TopLevel"":""false"",
                    ""DependencyIDs"": [ ""tfm1\\xxx\\topdependency1"" ]
                }",
                icon: KnownMonikers.Uninstall,
                expandedIcon: KnownMonikers.Uninstall);

            var previousSnapshot = new TargetedDependenciesSnapshot(
                "ProjectPath",
                TargetFramework.Any,
                catalogs: null,
                dependenciesWorld: new Dictionary<string, IDependency>()
                {
                    { dependencyTop1.Id, dependencyTop1 },
                    { dependencyTop2.Id, dependencyTop2 },
                }.ToImmutableDictionary(),
                topLevelDependencies: new List<IDependency>() { dependencyTop1 }.ToImmutableHashSet());

            // verify it doesn't stack overflow
            previousSnapshot.CheckForUnresolvedDependencies(dependencyTop1);   
        }

        internal enum FilterAction
        {
            Cancel,
            ShouldBeRemoved,
            ShouldBeAdded
        }

        internal class TestDependenciesSnapshotFilter : IDependenciesSnapshotFilter
        {
            private bool _filterAnyChanges;
            public TestDependenciesSnapshotFilter ImplementFilterAnyChanges(bool any)
            {
                _filterAnyChanges = any;

                return this;
            }

            public TestDependenciesSnapshotFilter ImplementBeforeAddResult(FilterAction action, string id, IDependency dependency)
            {
                _beforeAdd.Add(id, Tuple.Create(dependency, action));

                return this;
            }

            private readonly Dictionary<string, Tuple<IDependency, FilterAction>> _beforeAdd
                = new Dictionary<string, Tuple<IDependency, FilterAction>>(StringComparer.OrdinalIgnoreCase);

            public TestDependenciesSnapshotFilter ImplementBeforeRemoveResult(FilterAction action, string id, IDependency dependency)
            {
                _beforeRemove.Add(id, Tuple.Create(dependency, action));

                return this;
            }

            private readonly Dictionary<string, Tuple<IDependency, FilterAction>> _beforeRemove
                = new Dictionary<string, Tuple<IDependency, FilterAction>>(StringComparer.OrdinalIgnoreCase);

            public IDependency BeforeAdd(
                string projectPath,
                ITargetFramework targetFramework,
                IDependency dependency,
                ImmutableDictionary<string, IDependency>.Builder worldBuilder,
                ImmutableHashSet<IDependency>.Builder topLevelBuilder,
                IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
                IImmutableSet<string> projectItemSpecs,
                out bool filterAnyChanges)
            {
                filterAnyChanges = _filterAnyChanges;

                if (_beforeAdd.TryGetValue(dependency.Id, out Tuple<IDependency, FilterAction> info))
                {
                    if (info.Item2 == FilterAction.Cancel)
                    {
                        return null;
                    }
                    else if (info.Item2 == FilterAction.ShouldBeAdded)
                    {
                        worldBuilder.Remove(info.Item1.Id);
                        worldBuilder.Add(info.Item1.Id, info.Item1);

                        if (info.Item1.TopLevel)
                        {
                            topLevelBuilder.Remove(info.Item1);
                            topLevelBuilder.Add(info.Item1);
                        }

                        return info.Item1;
                    }
                    else
                    {
                        worldBuilder.Remove(dependency.Id);
                        topLevelBuilder.Remove(dependency);
                    }
                }

                return dependency;
            }

            public bool BeforeRemove(
                string projectPath,
                ITargetFramework targetFramework,
                IDependency dependency,
                ImmutableDictionary<string, IDependency>.Builder worldBuilder,
                ImmutableHashSet<IDependency>.Builder topLevelBuilder,
                out bool filterAnyChanges)
            {
                filterAnyChanges = _filterAnyChanges;

                if (_beforeRemove.TryGetValue(dependency.Id, out Tuple<IDependency, FilterAction> info))
                {
                    if (info.Item2 == FilterAction.Cancel)
                    {
                        return false;
                    }
                    else if (info.Item2 == FilterAction.ShouldBeAdded)
                    {
                        worldBuilder.Remove(info.Item1.Id);
                        worldBuilder.Add(info.Item1.Id, info.Item1);
                    }
                    else
                    {
                        worldBuilder.Remove(dependency.Id);
                        topLevelBuilder.Remove(dependency);
                    }
                }

                return true;
            }
        }
    }
}
