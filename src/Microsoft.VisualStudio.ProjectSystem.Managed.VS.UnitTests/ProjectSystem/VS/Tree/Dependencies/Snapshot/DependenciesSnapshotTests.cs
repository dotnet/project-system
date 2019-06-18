// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class DependenciesSnapshotTests
    {
        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("projectPath", () => new DependenciesSnapshot(projectPath: null, null, null));
            Assert.Throws<ArgumentNullException>("activeTarget", () => new DependenciesSnapshot("path", activeTarget: null, null));
            Assert.Throws<ArgumentNullException>("targets", () => new DependenciesSnapshot("path", TargetFramework.Any, null));
        }

        [Fact]
        public void Constructor()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var activeTarget = new TargetFramework("tfm1");

            var snapshot = new DependenciesSnapshot(
                projectPath,
                activeTarget,
                ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(activeTarget, snapshot.ActiveTarget);
            Assert.Empty(snapshot.Targets);
            Assert.False(snapshot.HasUnresolvedDependency);
            Assert.Null(snapshot.FindDependency("foo"));
        }

        [Fact]
        public void CreateEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            var snapshot = DependenciesSnapshot.CreateEmpty(projectPath);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(TargetFramework.Empty, snapshot.ActiveTarget);
            Assert.Empty(snapshot.Targets);
            Assert.False(snapshot.HasUnresolvedDependency);
            Assert.Null(snapshot.FindDependency("foo"));
        }

        [Fact]
        public void FromChanges_Empty_NoChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var activeTarget = new TargetFramework("tfm1");

            var previousSnapshot = new DependenciesSnapshot(
                projectPath,
                activeTarget,
                ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);

            var targetChanges = new DependenciesChangesBuilder();
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges> { [activeTarget] = targetChanges.Build() };

            var snapshot = DependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                activeTarget,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_Empty_ProjectPathChange()
        {
            const string previousProjectPath = @"c:\somefolder\someproject\a.csproj";
            const string newProjectPath = @"c:\somefolder\someproject\b.csproj";

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var activeTarget = new TargetFramework("tfm1");

            var previousSnapshot = new DependenciesSnapshot(
                previousProjectPath,
                activeTarget,
                ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);

            var targetChanges = new DependenciesChangesBuilder();
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges> { [activeTarget] = targetChanges.Build() };

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                activeTarget,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(newProjectPath, snapshot.ProjectPath);
            Assert.Same(activeTarget, snapshot.ActiveTarget);
            Assert.Same(previousSnapshot.Targets, snapshot.Targets);
        }

        [Fact]
        public void FromChanges_Empty_ProjectPathAndActiveTargetChange()
        {
            const string previousProjectPath = @"c:\somefolder\someproject\a.csproj";
            const string newProjectPath = @"c:\somefolder\someproject\b.csproj";

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousActiveTarget = new TargetFramework("tfm1");
            var newActiveTarget = new TargetFramework("tfm2");

            var previousSnapshot = new DependenciesSnapshot(
                previousProjectPath,
                previousActiveTarget,
                ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);

            var targetChanges = new DependenciesChangesBuilder();
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges>
            {
                [previousActiveTarget] = targetChanges.Build(),
                [newActiveTarget] = targetChanges.Build()
            };

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                newActiveTarget,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(newProjectPath, snapshot.ProjectPath);
            Assert.Same(newActiveTarget, snapshot.ActiveTarget);
            Assert.Same(previousSnapshot.Targets, snapshot.Targets);
        }

        [Fact]
        public void FromChanges_Empty_ProjectPathAndTargetChange()
        {
            const string previousProjectPath = @"c:\somefolder\someproject\a.csproj";
            const string newProjectPath = @"c:\somefolder\someproject\b.csproj";

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var activeTarget = new TargetFramework("tfm1");

            var previousSnapshot = new DependenciesSnapshot(
                previousProjectPath,
                activeTarget,
                ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty);

            var targetChanges = new DependenciesChangesBuilder();
            var model = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1"
            };
            targetChanges.Added(model);
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges> { [activeTarget] = targetChanges.Build() };

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                activeTarget,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(newProjectPath, snapshot.ProjectPath);
            Assert.Same(activeTarget, snapshot.ActiveTarget);
            Assert.NotSame(previousSnapshot.Targets, snapshot.Targets);
            Assert.Equal(@"tfm1\Xxx\dependency1", snapshot.Targets[activeTarget].DependenciesWorld.First().Value.Id);
        }
    }
}
