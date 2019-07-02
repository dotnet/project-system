// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class DependenciesSnapshotTests
    {
        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("projectPath", () => new DependenciesSnapshot(projectPath: null, null, null));
            Assert.Throws<ArgumentNullException>("activeTargetFramework", () => new DependenciesSnapshot("path", activeTargetFramework: null, null));
            Assert.Throws<ArgumentNullException>("dependenciesByTargetFramework", () => new DependenciesSnapshot("path", TargetFramework.Any, null));
        }

        [Fact]
        public void Constructor_ThrowsIfActiveTargetframeworkNotEmptyAndNotInDependenciesByTargetFramework()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var ex = Assert.Throws<ArgumentException>(() => new DependenciesSnapshot(
                projectPath,
                activeTargetFramework: targetFramework,
                ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty));

            Assert.StartsWith("Must contain activeTargetFramework (tfm1).", ex.Message);
        }

        [Fact]
        public void Constructor()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");

            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(projectPath, catalogs, targetFramework);

            var snapshot = new DependenciesSnapshot(
                projectPath,
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.ActiveTargetFramework);
            Assert.Same(dependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);
            Assert.False(snapshot.HasUnresolvedDependency);
            Assert.Null(snapshot.FindDependency("foo"));
        }

        [Fact]
        public void CreateEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            var snapshot = DependenciesSnapshot.CreateEmpty(projectPath);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(TargetFramework.Empty, snapshot.ActiveTargetFramework);
            Assert.Empty(snapshot.DependenciesByTargetFramework);
            Assert.False(snapshot.HasUnresolvedDependency);
            Assert.Null(snapshot.FindDependency("foo"));
        }

        [Fact]
        public void FromChanges_NoChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");
            var targetFrameworks = ImmutableArray<ITargetFramework>.Empty.Add(targetFramework);
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(projectPath, catalogs, targetFramework);

            var previousSnapshot = new DependenciesSnapshot(
                projectPath,
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            var targetChanges = new DependenciesChangesBuilder();
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges> { [targetFramework] = targetChanges.Build() };

            var snapshot = DependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                targetFrameworks,
                activeTargetFramework: targetFramework,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_ProjectPathChange()
        {
            const string previousProjectPath = @"c:\somefolder\someproject\a.csproj";
            const string newProjectPath = @"c:\somefolder\someproject\b.csproj";

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(previousProjectPath, catalogs, targetFramework);

            var previousSnapshot = new DependenciesSnapshot(
                previousProjectPath,
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            var targetChanges = new DependenciesChangesBuilder();
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges> { [targetFramework] = targetChanges.Build() };

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                targetFrameworks: ImmutableArray<ITargetFramework>.Empty.Add(targetFramework),
                activeTargetFramework: targetFramework,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(newProjectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.ActiveTargetFramework);
            Assert.NotSame(previousSnapshot.DependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);
            var targetedSnapshot = snapshot.DependenciesByTargetFramework.Single().Value;
            Assert.Equal(newProjectPath, targetedSnapshot.ProjectPath);
            Assert.Equal(targetFramework, targetedSnapshot.TargetFramework);
            Assert.Empty(targetedSnapshot.TopLevelDependencies);
            Assert.Empty(targetedSnapshot.DependenciesWorld);
            Assert.Same(catalogs, targetedSnapshot.Catalogs);
            Assert.Same(
                dependenciesByTargetFramework.Single().Value.DependenciesWorld,
                targetedSnapshot.DependenciesWorld);
        }

        [Fact]
        public void FromChanges_ProjectPathAndActiveTargetChange()
        {
            const string previousProjectPath = @"c:\somefolder\someproject\a.csproj";
            const string newProjectPath = @"c:\somefolder\someproject\b.csproj";

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework1 = new TargetFramework("tfm1");
            var targetFramework2 = new TargetFramework("tfm2");
            var targetFrameworks = ImmutableArray<ITargetFramework>.Empty.Add(targetFramework1).Add(targetFramework2);
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(previousProjectPath, catalogs, targetFramework1, targetFramework2);

            var previousSnapshot = new DependenciesSnapshot(
                previousProjectPath,
                activeTargetFramework: targetFramework1,
                dependenciesByTargetFramework);

            var targetChanges = new DependenciesChangesBuilder();
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges>
            {
                [targetFramework1] = targetChanges.Build(),
                [targetFramework2] = targetChanges.Build()
            };

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                targetFrameworks,
                activeTargetFramework: targetFramework2,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(newProjectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework2, snapshot.ActiveTargetFramework);
            Assert.NotSame(previousSnapshot.DependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);
            Assert.Equal(2, snapshot.DependenciesByTargetFramework.Count);
            var targetedSnapshot1 = snapshot.DependenciesByTargetFramework[targetFramework1];
            var targetedSnapshot2 = snapshot.DependenciesByTargetFramework[targetFramework2];
            Assert.Equal(newProjectPath, targetedSnapshot1.ProjectPath);
            Assert.Equal(newProjectPath, targetedSnapshot2.ProjectPath);
            Assert.Equal(targetFramework1, targetedSnapshot1.TargetFramework);
            Assert.Equal(targetFramework2, targetedSnapshot2.TargetFramework);
            Assert.Empty(targetedSnapshot1.TopLevelDependencies);
            Assert.Empty(targetedSnapshot2.TopLevelDependencies);
            Assert.Empty(targetedSnapshot1.DependenciesWorld);
            Assert.Empty(targetedSnapshot2.DependenciesWorld);
            Assert.Same(catalogs, targetedSnapshot1.Catalogs);
            Assert.Same(catalogs, targetedSnapshot2.Catalogs);
            Assert.Same(
                dependenciesByTargetFramework[targetFramework1].DependenciesWorld,
                targetedSnapshot1.DependenciesWorld);
            Assert.Same(
                dependenciesByTargetFramework[targetFramework2].DependenciesWorld,
                targetedSnapshot2.DependenciesWorld);
        }

        [Fact]
        public void FromChanges_ProjectPathAndTargetChange()
        {
            const string previousProjectPath = @"c:\somefolder\someproject\a.csproj";
            const string newProjectPath = @"c:\somefolder\someproject\b.csproj";

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(previousProjectPath, catalogs, targetFramework);

            var previousSnapshot = new DependenciesSnapshot(
                previousProjectPath,
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            var targetChanges = new DependenciesChangesBuilder();
            var model = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1"
            };
            targetChanges.Added(model);
            var changes = new Dictionary<ITargetFramework, IDependenciesChanges> { [targetFramework] = targetChanges.Build() };

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                changes.ToImmutableDictionary(),
                catalogs,
                targetFrameworks: ImmutableArray<ITargetFramework>.Empty.Add(targetFramework),
                activeTargetFramework: targetFramework,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(newProjectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.ActiveTargetFramework);
            Assert.NotSame(previousSnapshot.DependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);
            Assert.Equal(@"tfm1\Xxx\dependency1", snapshot.DependenciesByTargetFramework[targetFramework].DependenciesWorld.First().Value.Id);
        }

        [Fact]
        public void SetTargets_FromEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            ITargetFramework tfm1 = new TargetFramework("tfm1");
            ITargetFramework tfm2 = new TargetFramework("tfm2");

            var snapshot = DependenciesSnapshot.CreateEmpty(projectPath)
                .SetTargets(new[] { tfm1, tfm2 }.ToImmutableArray(), tfm1);

            Assert.Same(tfm1, snapshot.ActiveTargetFramework);
            Assert.Equal(2, snapshot.DependenciesByTargetFramework.Count);
            Assert.True(snapshot.DependenciesByTargetFramework.ContainsKey(tfm1));
            Assert.True(snapshot.DependenciesByTargetFramework.ContainsKey(tfm2));
        }

        [Fact]
        public void SetTargets_SameMembers_DifferentActive()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            ITargetFramework tfm1 = new TargetFramework("tfm1");
            ITargetFramework tfm2 = new TargetFramework("tfm2");

            var before = DependenciesSnapshot.CreateEmpty(projectPath)
                .SetTargets(new[] { tfm1, tfm2 }.ToImmutableArray(), tfm1);

            var after = before.SetTargets(new[] { tfm1, tfm2 }.ToImmutableArray(), tfm2);

            Assert.Same(tfm2, after.ActiveTargetFramework);
            Assert.Same(before.DependenciesByTargetFramework, after.DependenciesByTargetFramework);
        }

        [Fact]
        public void SetTargets_SameMembers_SameActive()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            ITargetFramework tfm1 = new TargetFramework("tfm1");
            ITargetFramework tfm2 = new TargetFramework("tfm2");

            var before = DependenciesSnapshot.CreateEmpty(projectPath)
                .SetTargets(new[] { tfm1, tfm2 }.ToImmutableArray(), tfm1);

            var after = before.SetTargets(new[] { tfm1, tfm2 }.ToImmutableArray(), tfm1);

            Assert.Same(before, after);
        }

        [Fact]
        public void SetTargets_DifferentMembers_DifferentActive()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            ITargetFramework tfm1 = new TargetFramework("tfm1");
            ITargetFramework tfm2 = new TargetFramework("tfm2");
            ITargetFramework tfm3 = new TargetFramework("tfm3");

            var before = DependenciesSnapshot.CreateEmpty(projectPath)
                .SetTargets(new[] { tfm1, tfm2 }.ToImmutableArray(), tfm1);

            var after = before.SetTargets(new[] { tfm2, tfm3 }.ToImmutableArray(), tfm3);

            Assert.Same(tfm3, after.ActiveTargetFramework);
            Assert.Equal(2, after.DependenciesByTargetFramework.Count);
            Assert.True(after.DependenciesByTargetFramework.ContainsKey(tfm2));
            Assert.True(after.DependenciesByTargetFramework.ContainsKey(tfm3));
            Assert.Same(before.DependenciesByTargetFramework[tfm2], after.DependenciesByTargetFramework[tfm2]);
        }

        private static ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> CreateDependenciesByTargetFramework(
            string projectPath,
            IProjectCatalogSnapshot catalogs,
            params ITargetFramework[] targetFrameworks)
        {
            var dic = ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty;

            foreach (var targetFramework in targetFrameworks)
            {
                dic = dic.Add(targetFramework, TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs));
            }

            return dic;
        }
    }
}
