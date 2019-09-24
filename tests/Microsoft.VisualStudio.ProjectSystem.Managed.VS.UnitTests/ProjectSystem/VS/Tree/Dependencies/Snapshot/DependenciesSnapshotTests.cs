// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
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
            var path = "path";
            var tfm = TargetFramework.Any;
            var dic = ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot>.Empty;

            Assert.Throws<ArgumentNullException>("projectPath",                   () => new DependenciesSnapshot(null!, tfm,   dic));
            Assert.Throws<ArgumentNullException>("activeTargetFramework",         () => new DependenciesSnapshot(path,  null!, dic));
            Assert.Throws<ArgumentNullException>("dependenciesByTargetFramework", () => new DependenciesSnapshot(path,  tfm,   null!));
        }

        [Fact]
        public void Constructor_ThrowsIfActiveTargetFrameworkNotEmptyAndNotInDependenciesByTargetFramework()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var ex = Assert.Throws<ArgumentException>(() => new DependenciesSnapshot(
                projectPath,
                activeTargetFramework: targetFramework,
                ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot>.Empty));

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
            Assert.False(snapshot.HasReachableVisibleUnresolvedDependency);
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
            Assert.False(snapshot.HasReachableVisibleUnresolvedDependency);
            Assert.Null(snapshot.FindDependency("foo"));
        }

        [Fact]
        public void FromChanges_NullChanges_Throws()
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

            Assert.Throws<ArgumentNullException>(
                "changes",
                () => DependenciesSnapshot.FromChanges(
                    projectPath,
                    previousSnapshot,
                    targetFramework,
                    null!,
                    catalogs,
                    targetFrameworks,
                    activeTargetFramework: targetFramework,
                    ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                    new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                    null));
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

            var snapshot = DependenciesSnapshot.FromChanges(
                newProjectPath,
                previousSnapshot,
                targetFramework,
                targetChanges.TryBuildChanges()!,
                catalogs,
                targetFrameworks: ImmutableArray.Create<ITargetFramework>(targetFramework),
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
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

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
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            var after = before.SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm2);

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
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            var after = before.SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

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
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            var after = before.SetTargets(ImmutableArray.Create(tfm2, tfm3), tfm3);

            Assert.Same(tfm3, after.ActiveTargetFramework);
            Assert.Equal(2, after.DependenciesByTargetFramework.Count);
            Assert.True(after.DependenciesByTargetFramework.ContainsKey(tfm2));
            Assert.True(after.DependenciesByTargetFramework.ContainsKey(tfm3));
            Assert.Same(before.DependenciesByTargetFramework[tfm2], after.DependenciesByTargetFramework[tfm2]);
        }

        private static ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> CreateDependenciesByTargetFramework(
            string projectPath,
            IProjectCatalogSnapshot catalogs,
            params ITargetFramework[] targetFrameworks)
        {
            var dic = ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot>.Empty;

            foreach (var targetFramework in targetFrameworks)
            {
                dic = dic.Add(targetFramework, TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs));
            }

            return dic;
        }
    }
}
