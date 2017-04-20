// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class TargetedDependenciesSnapshotTests
    {
        [Fact]
        public void TargetedDependenciesSnapshot_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("projectPath", () =>
            {
                new TestableTargetedDependenciesSnapshot(null, null);
            });

            Assert.Throws<ArgumentNullException>("targetFramework", () =>
            {
                new TestableTargetedDependenciesSnapshot("someprojectpath", null);
            });
        }

        [Fact]
        public void TargetedDependenciesSnapshot_Constructor()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld:new Dictionary<string, IDependency>(),
                topLevelDependencies:new List<IDependency>());

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = new TestableTargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                previousSnapshot,
                catalogs);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.Equal(previousSnapshot.TopLevelDependencies, snapshot.TopLevelDependencies);
            Assert.Equal(previousSnapshot.DependenciesWorld, snapshot.DependenciesWorld);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_Empty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>(),
                topLevelDependencies: new List<IDependency>());

            var addedNodes = new List<IDependencyModel>();
            var removedNodes = new List<IDependencyModel>();
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters = null;

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                snapshotFilters,
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.False(anyChanges);
            Assert.Equal(0, snapshot.TopLevelDependencies.Count);
            Assert.Equal(0, snapshot.DependenciesWorld.Count);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_NoChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyModelTop1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelChild1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\childdependency1"",
    ""Name"":""ChildDependency1"",
    ""Caption"":""ChildDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>()
                {
                    { dependencyModelTop1.Id, dependencyModelTop1 },
                    { dependencyModelChild1.Id, dependencyModelChild1 },
                },
                topLevelDependencies: new List<IDependency>() { dependencyModelTop1 });

            var addedNodes = new List<IDependencyModel>();
            var removedNodes = new List<IDependencyModel>();
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters = null;

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                snapshotFilters,
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.False(anyChanges);
            Assert.Equal(1, snapshot.TopLevelDependencies.Count);
            Assert.Equal(2, snapshot.DependenciesWorld.Count);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_NoChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyModelTop1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelChild1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\childdependency1"",
    ""Name"":""ChildDependency1"",
    ""Caption"":""ChildDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>()
                {
                    { dependencyModelTop1.Id, dependencyModelTop1 },
                    { dependencyModelChild1.Id, dependencyModelChild1 },
                },
                topLevelDependencies: new List<IDependency>() { dependencyModelTop1 });

            var addedNodes = new List<IDependencyModel>();
            var removedNodes = new List<IDependencyModel> { dependencyModelTop1 };
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeRemoveResult(FilterAction.Cancel, @"tfm1\xxx\newdependency1", null);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.False(anyChanges);
            Assert.Equal(1, snapshot.TopLevelDependencies.Count);
            Assert.Equal(2, snapshot.DependenciesWorld.Count);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_ReportedChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyModelTop1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelChild1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\childdependency1"",
    ""Name"":""ChildDependency1"",
    ""Caption"":""ChildDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelTop1Removed = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase)
                {
                    { dependencyModelTop1.Id, dependencyModelTop1 },
                    { dependencyModelChild1.Id, dependencyModelChild1 },
                },
                topLevelDependencies: new List<IDependency>() { dependencyModelTop1 });

            var addedNodes = new List<IDependencyModel>();
            var removedNodes = new List<IDependencyModel> { dependencyModelTop1Removed };
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeRemoveResult(FilterAction.Cancel, @"tfm1\xxx\topdependency1", null)
                    .ImplementFilterAnyChanges(true);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.True(anyChanges);
            Assert.Equal(1, snapshot.TopLevelDependencies.Count);
            Assert.Equal(2, snapshot.DependenciesWorld.Count);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_NoChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyModelTop1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelChild1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\childdependency1"",
    ""Name"":""ChildDependency1"",
    ""Caption"":""ChildDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelNew1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""newdependency1"",
    ""Name"":""NewDependency1"",
    ""Caption"":""NewDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>()
                {
                    { dependencyModelTop1.Id, dependencyModelTop1 },
                    { dependencyModelChild1.Id, dependencyModelChild1 },
                },
                topLevelDependencies: new List<IDependency>() { dependencyModelTop1 });

            var addedNodes = new List<IDependencyModel> { dependencyModelNew1 };
            var removedNodes = new List<IDependencyModel>();
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeAddResult(FilterAction.Cancel, @"tfm1\xxx\newdependency1", null);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.False(anyChanges);
            Assert.Equal(1, snapshot.TopLevelDependencies.Count);
            Assert.Equal(2, snapshot.DependenciesWorld.Count);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_ReportedChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyModelTop1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelChild1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\childdependency1"",
    ""Name"":""ChildDependency1"",
    ""Caption"":""ChildDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelNew1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""newdependency1"",
    ""Name"":""NewDependency1"",
    ""Caption"":""NewDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);
            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>()
                {
                    { dependencyModelTop1.Id, dependencyModelTop1 },
                    { dependencyModelChild1.Id, dependencyModelChild1 },
                },
                topLevelDependencies: new List<IDependency>() { dependencyModelTop1 });

            var addedNodes = new List<IDependencyModel> { dependencyModelNew1 };
            var removedNodes = new List<IDependencyModel>();
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .ImplementBeforeAddResult(FilterAction.Cancel, @"tfm1\xxx\newdependency1", null)
                    .ImplementFilterAnyChanges(true);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.True(anyChanges);
            Assert.Equal(1, snapshot.TopLevelDependencies.Count);
            Assert.Equal(2, snapshot.DependenciesWorld.Count);
        }

        [Fact]
        public void TargetedDependenciesSnapshot_FromChanges_RemovedAndAddedChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");

            var dependencyModelTop1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""topdependency1"",
    ""Name"":""TopDependency1"",
    ""Caption"":""TopDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelChild1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""childdependency1"",
    ""Name"":""ChildDependency1"",
    ""Caption"":""ChildDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelAdded1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""addeddependency1"",
    ""Name"":""AddedDependency1"",
    ""Caption"":""AddedDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelAdded2 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""addeddependency2"",
    ""Name"":""AddedDependency2"",
    ""Caption"":""AddedDependency2"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelAdded3 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""addeddependency3"",
    ""Name"":""AddedDependency3"",
    ""Caption"":""AddedDependency3"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true"",
    ""TopLevel"":""false""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyAdded2Changed = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\addeddependency2"",
    ""Name"":""AddedDependency2Changed"",
    ""Caption"":""AddedDependency2Changed"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyRemoved1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\Removeddependency1"",
    ""Name"":""RemovedDependency1"",
    ""Caption"":""RemovedDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyModelRemoved1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""Removeddependency1"",
    ""Name"":""RemovedDependency1"",
    ""Caption"":""RemovedDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var dependencyInsteadRemoved1 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Xxx"",
    ""Id"": ""tfm1\\xxx\\InsteadRemoveddependency1"",
    ""Name"":""InsteadRemovedDependency1"",
    ""Caption"":""InsteadRemovedDependency1"",
    ""SchemaItemType"":""Xxx"",
    ""Resolved"":""true""
}", icon: KnownMonikers.Uninstall, expandedIcon: KnownMonikers.Uninstall);

            var previousSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                dependenciesWorld: new Dictionary<string, IDependency>()
                {
                    { @"tfm1\xxx\topdependency1", dependencyModelTop1 },
                    { @"tfm1\xxx\childdependency1", dependencyModelChild1 },
                    { @"tfm1\xxx\Removeddependency1", dependencyRemoved1 },
                },
                topLevelDependencies: new List<IDependency>() { dependencyModelTop1 });

            var addedNodes = new List<IDependencyModel> { dependencyModelAdded1, dependencyModelAdded2, dependencyModelAdded3 };
            var removedNodes = new List<IDependencyModel> { dependencyModelRemoved1 };
            var changes = IDependenciesChangesFactory.Implement(addedNodes: addedNodes, removedNodes: removedNodes);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                                        .ImplementBeforeAddResult(FilterAction.Cancel, @"tfm1\xxx\addeddependency1", null)
                                        .ImplementBeforeAddResult(FilterAction.ShouldBeAdded, @"tfm1\xxx\addeddependency2", dependencyAdded2Changed)
                                        .ImplementBeforeRemoveResult(FilterAction.ShouldBeAdded, @"tfm1\xxx\Removeddependency1", dependencyInsteadRemoved1);

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                targetFramework,
                previousSnapshot,
                changes,
                catalogs,
                new[] { snapshotFilter },
                null,
                null,
                out bool anyChanges);

            Assert.NotNull(snapshot.TargetFramework);
            Assert.Equal("tfm1", snapshot.TargetFramework.Moniker);
            Assert.Equal(projectPath, snapshot.ProjectPath);
            Assert.Equal(catalogs, snapshot.Catalogs);
            Assert.True(anyChanges);
            Assert.Equal(2, snapshot.TopLevelDependencies.Count);
            Assert.True(snapshot.TopLevelDependencies.Any(x => x.Id.Equals(@"topdependency1")));
            Assert.True(snapshot.TopLevelDependencies.Any(x => x.Id.Equals(@"tfm1\xxx\addeddependency2") && x.Caption.Equals("AddedDependency2Changed")));
            Assert.Equal(5, snapshot.DependenciesWorld.Count);
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\topdependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\childdependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\addeddependency2"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\InsteadRemoveddependency1"));
            Assert.True(snapshot.DependenciesWorld.ContainsKey(@"tfm1\xxx\addeddependency3"));
        }

        private class TestableTargetedDependenciesSnapshot : TargetedDependenciesSnapshot
        {
            public TestableTargetedDependenciesSnapshot(
                string projectPath,
                ITargetFramework targetFramework,
                ITargetedDependenciesSnapshot previousSnapshot = null,
                IProjectCatalogSnapshot catalogs = null)
                : base(projectPath, targetFramework, previousSnapshot, catalogs)
            {
            }
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

            private Dictionary<string, Tuple<IDependency, FilterAction>> _beforeAdd 
                = new Dictionary<string, Tuple<IDependency, FilterAction>>(StringComparer.OrdinalIgnoreCase);

            public TestDependenciesSnapshotFilter ImplementBeforeRemoveResult(FilterAction action, string id, IDependency dependency)
            {
                _beforeRemove.Add(id, Tuple.Create(dependency, action));

                return this;
            }

            private Dictionary<string, Tuple<IDependency, FilterAction>> _beforeRemove
                = new Dictionary<string, Tuple<IDependency, FilterAction>>(StringComparer.OrdinalIgnoreCase);

            public IDependency BeforeAdd(
                string projectPath,
                ITargetFramework targetFramework,
                IDependency dependency,
                ImmutableDictionary<string, IDependency>.Builder worldBuilder,
                ImmutableHashSet<IDependency>.Builder topLevelBuilder,
                Dictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
                HashSet<string> projectItemSpecs,
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

            public IDependency BeforeRemove(
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
                        return null;
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

                return dependency;
            }
        }
    }
}
