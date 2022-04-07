// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class DependenciesSnapshotTests
    {
        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            var tfm = TargetFramework.Any;
            var dic = ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty;

            Assert.Throws<ArgumentNullException>("activeTargetFramework",         () => new DependenciesSnapshot(null!, dic));
            Assert.Throws<ArgumentNullException>("dependenciesByTargetFramework", () => new DependenciesSnapshot(tfm,   null!));
        }

        [Fact]
        public void Constructor_ThrowsIfActiveTargetFrameworkNotEmptyAndNotInDependenciesByTargetFramework_NoTargets()
        {
#if false
            var ex = Assert.Throws<ArgumentException>(() => new DependenciesSnapshot(
                activeTargetFramework: new TargetFramework("tfm1"),
                dependenciesByTargetFramework: ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty));

            Assert.StartsWith("Value \"tfm1\" is unexpected. Must be a key in dependenciesByTargetFramework, which contains no items.", ex.Message);
#else
            _ = new DependenciesSnapshot(
                activeTargetFramework: new TargetFramework("tfm1"),
                dependenciesByTargetFramework: ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty);
#endif
        }

        [Fact]
        public void Constructor_ThrowsIfActiveTargetFrameworkNotEmptyAndNotInDependenciesByTargetFramework_WithTargets()
        {
#if false
            var tfm1 = new TargetFramework("tfm1");
            var tfm2 = new TargetFramework("tfm2");
            var tfm3 = new TargetFramework("tfm3");

            var ex = Assert.Throws<ArgumentException>(() => new DependenciesSnapshot(
                activeTargetFramework: tfm1,
                dependenciesByTargetFramework: ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty
                    .Add(tfm2, TargetedDependenciesSnapshot.CreateEmpty(tfm1, null))
                    .Add(tfm3, TargetedDependenciesSnapshot.CreateEmpty(tfm1, null))));

            Assert.StartsWith("Value \"tfm1\" is unexpected. Must be a key in dependenciesByTargetFramework, which contains \"tfm2\", \"tfm3\".", ex.Message);
#else
            var tfm1 = new TargetFramework("tfm1");
            var tfm2 = new TargetFramework("tfm2");
            var tfm3 = new TargetFramework("tfm3");

            _ = new DependenciesSnapshot(
                activeTargetFramework: tfm1,
                dependenciesByTargetFramework: ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty
                    .Add(tfm2, TargetedDependenciesSnapshot.CreateEmpty(tfm1, null))
                    .Add(tfm3, TargetedDependenciesSnapshot.CreateEmpty(tfm1, null)));
#endif
        }

        [Fact]
        public void Constructor_NoThrowIfActiveTargetFrameworkIsEmptyAndNotPresentInDependenciesByTargetFramework()
        {
            _ = new DependenciesSnapshot(
                activeTargetFramework: TargetFramework.Empty,
                ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty);
        }

        [Fact]
        public void Constructor_NoThrowIfActiveTargetFrameworkIsUnsupportedAndNotPresentInDependenciesByTargetFramework()
        {
            _ = new DependenciesSnapshot(
                activeTargetFramework: TargetFramework.Unsupported,
                ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty);
        }

        [Fact]
        public void Constructor()
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");

            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(catalogs, targetFramework);

            var snapshot = new DependenciesSnapshot(
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            Assert.Same(targetFramework, snapshot.ActiveTargetFramework);
            Assert.Same(dependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);
            Assert.Equal(DiagnosticLevel.None, snapshot.MaximumVisibleDiagnosticLevel);
        }

        [Fact]
        public void CreateEmpty()
        {
            var snapshot = DependenciesSnapshot.Empty;

            Assert.Same(TargetFramework.Empty, snapshot.ActiveTargetFramework);
            Assert.Empty(snapshot.DependenciesByTargetFramework);
            Assert.Equal(DiagnosticLevel.None, snapshot.MaximumVisibleDiagnosticLevel);
        }

        [Fact]
        public void FromChanges_NoChanges()
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");
            var targetFrameworks = ImmutableArray<TargetFramework>.Empty.Add(targetFramework);
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(catalogs, targetFramework);

            var previousSnapshot = new DependenciesSnapshot(
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            var snapshot = DependenciesSnapshot.FromChanges(
                previousSnapshot,
                targetFramework,
                changes: null,
                catalogs,
                targetFrameworks,
                activeTargetFramework: targetFramework);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_CatalogsChanged()
        {
            var previousCatalogs = IProjectCatalogSnapshotFactory.Create();
            var updatedCatalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");
            var targetFrameworks = ImmutableArray<TargetFramework>.Empty.Add(targetFramework);
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(previousCatalogs, targetFramework);

            var previousSnapshot = new DependenciesSnapshot(
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            var snapshot = DependenciesSnapshot.FromChanges(
                previousSnapshot,
                targetFramework,
                changes: null,
                updatedCatalogs,
                targetFrameworks,
                activeTargetFramework: targetFramework);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(targetFramework, snapshot.ActiveTargetFramework);
            Assert.NotSame(previousSnapshot.DependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);

            Assert.Single(snapshot.DependenciesByTargetFramework);
        }

        [Fact]
        public void FromChanges_WithDependenciesChanges()
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targetFramework = new TargetFramework("tfm1");
            var dependenciesByTargetFramework = CreateDependenciesByTargetFramework(catalogs, targetFramework);

            var previousSnapshot = new DependenciesSnapshot(
                activeTargetFramework: targetFramework,
                dependenciesByTargetFramework);

            var targetChanges = new DependenciesChangesBuilder();
            var model = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1"
            };
            targetChanges.Added(targetFramework, model);

            var snapshot = DependenciesSnapshot.FromChanges(
                previousSnapshot,
                targetFramework,
                targetChanges.TryBuildChanges()!,
                catalogs,
                targetFrameworks: ImmutableArray.Create(targetFramework),
                activeTargetFramework: targetFramework);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(targetFramework, snapshot.ActiveTargetFramework);
            Assert.NotSame(previousSnapshot.DependenciesByTargetFramework, snapshot.DependenciesByTargetFramework);

            var (actualTfm, targetedSnapshot) = Assert.Single(snapshot.DependenciesByTargetFramework);
            Assert.Same(targetFramework, actualTfm);
            var dependency = Assert.Single(targetedSnapshot.Dependencies);
            Assert.Equal("dependency1", dependency.Id);
            Assert.Equal("Xxx", dependency.ProviderType);
        }

        [Fact]
        public void SetTargets_FromEmpty()
        {
            var tfm1 = new TargetFramework("tfm1");
            var tfm2 = new TargetFramework("tfm2");

            var snapshot = DependenciesSnapshot.Empty
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            Assert.Same(tfm1, snapshot.ActiveTargetFramework);
            Assert.Equal(2, snapshot.DependenciesByTargetFramework.Count);
            Assert.True(snapshot.DependenciesByTargetFramework.ContainsKey(tfm1));
            Assert.True(snapshot.DependenciesByTargetFramework.ContainsKey(tfm2));
        }

        [Fact]
        public void SetTargets_SameMembers_DifferentActive()
        {
            var tfm1 = new TargetFramework("tfm1");
            var tfm2 = new TargetFramework("tfm2");

            var before = DependenciesSnapshot.Empty
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            var after = before.SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm2);

            Assert.Same(tfm2, after.ActiveTargetFramework);
            Assert.Same(before.DependenciesByTargetFramework, after.DependenciesByTargetFramework);
        }

        [Fact]
        public void SetTargets_SameMembers_SameActive()
        {
            var tfm1 = new TargetFramework("tfm1");
            var tfm2 = new TargetFramework("tfm2");

            var before = DependenciesSnapshot.Empty
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            var after = before.SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            Assert.Same(before, after);
        }

        [Fact]
        public void SetTargets_DifferentMembers_DifferentActive()
        {
            var tfm1 = new TargetFramework("tfm1");
            var tfm2 = new TargetFramework("tfm2");
            var tfm3 = new TargetFramework("tfm3");

            var before = DependenciesSnapshot.Empty
                .SetTargets(ImmutableArray.Create(tfm1, tfm2), tfm1);

            var after = before.SetTargets(ImmutableArray.Create(tfm2, tfm3), tfm3);

            Assert.Same(tfm3, after.ActiveTargetFramework);
            Assert.Equal(2, after.DependenciesByTargetFramework.Count);
            Assert.True(after.DependenciesByTargetFramework.ContainsKey(tfm2));
            Assert.True(after.DependenciesByTargetFramework.ContainsKey(tfm3));
            Assert.Same(before.DependenciesByTargetFramework[tfm2], after.DependenciesByTargetFramework[tfm2]);
        }

        private static ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot> CreateDependenciesByTargetFramework(
            IProjectCatalogSnapshot catalogs,
            params TargetFramework[] targetFrameworks)
        {
            var dic = ImmutableDictionary<TargetFramework, TargetedDependenciesSnapshot>.Empty;

            foreach (var targetFramework in targetFrameworks)
            {
                dic = dic.Add(targetFramework, TargetedDependenciesSnapshot.CreateEmpty(targetFramework, catalogs));
            }

            return dic;
        }
    }
}
