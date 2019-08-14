// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class TargetedDependenciesSnapshotTests
    {
        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            var path = "path";
            var tfm = TargetFramework.Any;
            var deps = ImmutableDictionary<string, IDependency>.Empty;

            Assert.Throws<ArgumentNullException>("projectPath",       () => new TargetedDependenciesSnapshot(null!, tfm,   null, deps));
            Assert.Throws<ArgumentNullException>("targetFramework",   () => new TargetedDependenciesSnapshot(path,  null!, null, deps));
            Assert.Throws<ArgumentNullException>("dependenciesWorld", () => new TargetedDependenciesSnapshot(path,  tfm,   null, null!));
        }

        [Fact]
        public void Constructor()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableDictionary<string, IDependency>.Empty);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.TargetFramework);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.False(snapshot.HasVisibleUnresolvedDependency);
            Assert.Empty(snapshot.TopLevelDependencies);
            Assert.Empty(snapshot.DependenciesWorld);
            Assert.False(snapshot.CheckForUnresolvedDependencies("foo"));
            Assert.Empty(snapshot.GetDependencyChildren(new TestDependency()));
        }

        [Fact]
        public void CreateEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");
            var catalogs = IProjectCatalogSnapshotFactory.Create();

            var snapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.TargetFramework);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.False(snapshot.HasVisibleUnresolvedDependency);
            Assert.Empty(snapshot.TopLevelDependencies);
            Assert.Empty(snapshot.DependenciesWorld);
            Assert.False(snapshot.CheckForUnresolvedDependencies("foo"));
            Assert.Empty(snapshot.GetDependencyChildren(new TestDependency()));
        }

        [Fact]
        public void FromChanges_Empty_NoChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            var changes = new DependenciesChangesBuilder();

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_NotEmpty_NoChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyTop1 = new TestDependency
            {
                Id = @"tfm1\xxx\topdependency1",
                ProviderType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new [] { dependencyTop1 },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = new DependenciesChangesBuilder();

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_AddingToEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            var resolvedTop = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                Resolved = true,
                TopLevel = true,
                Flags = DependencyTreeFlags.Resolved,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var unresolved = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency2",
                Name = "Dependency2",
                Caption = "Dependency2",
                Resolved = false,
                TopLevel = false,
                Flags = DependencyTreeFlags.Unresolved,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var changes = new DependenciesChangesBuilder();
            changes.Added(resolvedTop);
            changes.Added(unresolved);

            const string updatedProjectPath = "updatedProjectPath";

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                updatedProjectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(updatedProjectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.True(snapshot.HasVisibleUnresolvedDependency);
            AssertEx.CollectionLength(snapshot.DependenciesWorld, 2);
            AssertEx.CollectionLength(snapshot.TopLevelDependencies, 1);
            Assert.True(resolvedTop.Matches(snapshot.TopLevelDependencies.Single(), targetFramework));
            Assert.True(resolvedTop.Matches(snapshot.DependenciesWorld["tfm1\\Xxx\\dependency1"], targetFramework));
            Assert.True(unresolved.Matches(snapshot.DependenciesWorld["tfm1\\Xxx\\dependency2"], targetFramework));
        }

        [Fact]
        public void FromChanges_NoChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            var targetFramework = new TargetFramework("tfm1");

            var dependencyTop1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "topdependency1"),
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var dependencyChild1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "childdependency1"),
                Name = "ChildDependency1",
                Caption = "ChildDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new [] { dependencyTop1, dependencyChild1 },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = new DependenciesChangesBuilder();
            changes.Removed(dependencyTop1.ProviderType, dependencyTop1.Id);

            var snapshotFilter = new TestDependenciesSnapshotFilter();

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_ReportedChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyTop1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\topdependency1",
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var dependencyChild1 = new TestDependency
            {
                ProviderType =  "Xxx",
                Id =  "tfm1\\xxx\\childdependency1",
                Name = "ChildDependency1",
                Caption = "ChildDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new [] { dependencyTop1, dependencyChild1 },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = new DependenciesChangesBuilder();
            changes.Removed("Xxx", "topdependency1");

            var addedOnRemove = new TestDependency { Id = "SomethingElse", TopLevel = false };

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeRemoveReject(@"tfm1\xxx\topdependency1", addOrUpdate: addedOnRemove);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.Single(snapshot.TopLevelDependencies);
            AssertEx.CollectionLength(snapshot.DependenciesWorld, 3);
            Assert.Contains(addedOnRemove, snapshot.DependenciesWorld.Values);
        }

        [Fact]
        public void FromChanges_NoChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyTop1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "topdependency1"),
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var dependencyChild1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "childdependency1"),
                Name = "ChildDependency1",
                Caption = "ChildDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            var dependencyModelNew1 = new TestDependencyModel
            {
                ProviderType =  "Xxx",
                Id =  "newdependency1",
                Name = "NewDependency1",
                Caption = "NewDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new [] { dependencyTop1, dependencyChild1 },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelNew1);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeAddReject(@"tfm1\xxx\newdependency1");

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_ReportedChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyTop1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\topdependency1",
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var dependencyChild1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\childdependency1",
                Name = "ChildDependency1",
                Caption = "ChildDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            var dependencyModelNew1 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "newdependency1",
                Name = "NewDependency1",
                Caption = "NewDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new [] { dependencyTop1, dependencyChild1 },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelNew1);

            var filterAddedDependency = new TestDependency { Id = "unexpected", TopLevel = true };

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeAddReject(@"tfm1\xxx\newdependency1", addOrUpdate: filterAddedDependency);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(previousSnapshot.ProjectPath, snapshot.ProjectPath);
            Assert.Same(previousSnapshot.Catalogs, snapshot.Catalogs);

            AssertEx.CollectionLength(snapshot.TopLevelDependencies, 2);
            Assert.Contains(dependencyTop1, snapshot.TopLevelDependencies);
            Assert.Contains(filterAddedDependency, snapshot.TopLevelDependencies);

            AssertEx.CollectionLength(snapshot.DependenciesWorld, 3);
            Assert.Contains(dependencyTop1, snapshot.DependenciesWorld.Values);
            Assert.Contains(dependencyChild1, snapshot.DependenciesWorld.Values);
            Assert.Contains(filterAddedDependency, snapshot.DependenciesWorld.Values);
        }

        [Fact]
        public void FromChanges_RemovedAndAddedChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyTop1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\topdependency1",
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var dependencyChild1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\childdependency1",
                Name = "ChildDependency1",
                Caption = "ChildDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            var dependencyModelAdded1 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "addeddependency1",
                Name = "AddedDependency1",
                Caption = "AddedDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyModelAdded2 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "addeddependency2",
                Name = "AddedDependency2",
                Caption = "AddedDependency2",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyModelAdded3 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "addeddependency3",
                Name = "AddedDependency3",
                Caption = "AddedDependency3",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyAdded2Changed = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\addeddependency2",
                Name = "AddedDependency2Changed",
                Caption = "AddedDependency2Changed",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = true
            };

            var dependencyRemoved1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\Removeddependency1",
                Name = "RemovedDependency1",
                Caption = "RemovedDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            var dependencyInsteadRemoved1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\InsteadRemoveddependency1",
                Name = "InsteadRemovedDependency1",
                Caption = "InsteadRemovedDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                TopLevel = false
            };

            Assert.True(dependencyTop1.TopLevel);
            Assert.False(dependencyChild1.TopLevel);
            Assert.False(dependencyRemoved1.TopLevel);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new [] { dependencyTop1, dependencyChild1, dependencyRemoved1 },
                topLevelDependencies: new [] { dependencyTop1 });

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelAdded1);
            changes.Added(dependencyModelAdded2);
            changes.Added(dependencyModelAdded3);
            changes.Removed("Xxx", "Removeddependency1");

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeAddReject(@"tfm1\xxx\addeddependency1")
                .BeforeAddAccept(@"tfm1\xxx\addeddependency2", dependencyAdded2Changed)
                .BeforeAddAccept(@"tfm1\xxx\addeddependency3")
                .BeforeRemoveAccept(@"tfm1\xxx\Removeddependency1", dependencyInsteadRemoved1);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            AssertEx.CollectionLength(snapshot.TopLevelDependencies, 2);
            Assert.Contains(snapshot.TopLevelDependencies, x => x.Id.Equals(@"tfm1\xxx\topdependency1"));
            Assert.Contains(snapshot.TopLevelDependencies, x => x.Id.Equals(@"tfm1\xxx\addeddependency2") && x.Caption.Equals("AddedDependency2Changed"));
            AssertEx.CollectionLength(snapshot.DependenciesWorld, 5);
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\topdependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\childdependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\addeddependency2"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\InsteadRemoveddependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\addeddependency3"));
        }

        [Fact]
        public void FromChanges_UpdatesTopLevelDependencies()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyTopPrevious = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\topdependency1",
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelTopAdded = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "topdependency1",
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyTopUpdated = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\topdependency1",
                Name = "TopDependency1",
                Caption = "TopDependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                projectPath: projectPath,
                targetFramework: targetFramework,
                catalogs: catalogs,
                dependenciesWorld: new[] { dependencyTopPrevious },
                topLevelDependencies: new[] { dependencyTopPrevious });

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelTopAdded);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .BeforeAddAccept(@"tfm1\xxx\topdependency1", dependencyTopUpdated);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.Build(),
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
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
        public void CheckForUnresolvedDependencies_CircularDependency_DoesNotRecurseInfinitely()
        {
            const string id1 = @"tfm1\xxx\dependency1";
            const string id2 = @"tfm1\xxx\dependency2";
            const string providerType = "Xxx";

            var dependency1 = new TestDependency
            {
                Id = id1,
                ProviderType = providerType,
                TopLevel = true,
                DependencyIDs = ImmutableArray.Create(id2)
            };

            var dependency2 = new TestDependency
            {
                Id = id2,
                ProviderType = providerType,
                TopLevel = true,
                DependencyIDs = ImmutableArray.Create(id1)
            };

            var snapshot = new TargetedDependenciesSnapshot(
                "ProjectPath",
                TargetFramework.Any,
                catalogs: null,
                dependenciesWorld: new IDependency[] { dependency1, dependency2 }.ToDictionary(d => d.Id).ToImmutableDictionary());

            // verify it doesn't stack overflow
            snapshot.CheckForUnresolvedDependencies(dependency1);   
        }

        internal sealed class TestDependenciesSnapshotFilter : IDependenciesSnapshotFilter
        {
            private enum FilterAction { Reject, Accept }

            private readonly Dictionary<string, (FilterAction, IDependency?)> _beforeAdd    = new Dictionary<string, (FilterAction, IDependency?)>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, (FilterAction, IDependency?)> _beforeRemove = new Dictionary<string, (FilterAction, IDependency?)>(StringComparer.OrdinalIgnoreCase);

            public TestDependenciesSnapshotFilter BeforeAddAccept(string id, IDependency? dependency = null)
            {
                _beforeAdd.Add(id, (FilterAction.Accept, dependency));
                return this;
            }

            public TestDependenciesSnapshotFilter BeforeAddReject(string id, IDependency? addOrUpdate = null)
            {
                _beforeAdd.Add(id, (FilterAction.Reject, addOrUpdate));
                return this;
            }

            public TestDependenciesSnapshotFilter BeforeRemoveAccept(string id, IDependency? addOrUpdate = null)
            {
                _beforeRemove.Add(id, (FilterAction.Accept, addOrUpdate));
                return this;
            }

            public TestDependenciesSnapshotFilter BeforeRemoveReject(string id, IDependency? addOrUpdate = null)
            {
                _beforeRemove.Add(id, (FilterAction.Reject, addOrUpdate));
                return this;
            }

            public void BeforeAddOrUpdate(
                ITargetFramework targetFramework,
                IDependency dependency,
                IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
                IImmutableSet<string>? projectItemSpecs,
                IAddDependencyContext context)
            {
                if (_beforeAdd.TryGetValue(dependency.Id, out (FilterAction Action, IDependency? Dependency) info))
                {
                    if (info.Action == FilterAction.Reject)
                    {
                        context.Reject();

                        if (info.Dependency != null)
                        {
                            context.AddOrUpdate(info.Dependency);
                        }
                    }
                    else if (info.Action == FilterAction.Accept)
                    {
                        context.Accept(info.Dependency ?? dependency);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new ArgumentException("Unexpected dependency ID: " + dependency.Id);
                }
            }

            public void BeforeRemove(
                ITargetFramework targetFramework,
                IDependency dependency,
                IRemoveDependencyContext context)
            {
                if (_beforeRemove.TryGetValue(dependency.Id, out (FilterAction Action, IDependency? Dependency) info))
                {
                    if (info.Action == FilterAction.Reject)
                    {
                        context.Reject();

                        if (info.Dependency != null)
                        {
                            context.AddOrUpdate(info.Dependency);
                        }
                    }
                    else if (info.Action == FilterAction.Accept)
                    {
                        context.Accept();

                        if (info.Dependency != null)
                        {
                            context.AddOrUpdate(info.Dependency);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new ArgumentException("Unexpected dependency ID: " + dependency.Id);
                }
            }
        }
    }
}
