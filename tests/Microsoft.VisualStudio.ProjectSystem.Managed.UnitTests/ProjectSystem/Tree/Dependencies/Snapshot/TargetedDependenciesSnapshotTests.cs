// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class TargetedDependenciesSnapshotTests
    {
        private readonly IProjectCatalogSnapshot _catalogs = IProjectCatalogSnapshotFactory.Create();
        private readonly TargetFramework _tfm1 = new("tfm1");

        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            var tfm = TargetFramework.Any;
            var deps = ImmutableArray<IDependency>.Empty;

            Assert.Throws<ArgumentNullException>("targetFramework", () => new TargetedDependenciesSnapshot(null!, null, deps));
            Assert.Throws<ArgumentException>("dependencies",        () => new TargetedDependenciesSnapshot(tfm,   null, default));
        }

        [Fact]
        public void Constructor()
        {
            var snapshot = new TargetedDependenciesSnapshot(
                _tfm1,
                _catalogs,
                ImmutableArray<IDependency>.Empty);

            Assert.Same(_tfm1, snapshot.TargetFramework);
            Assert.Same(_catalogs, snapshot.Catalogs);
            Assert.Equal(DiagnosticLevel.None, snapshot.MaximumVisibleDiagnosticLevel);
            Assert.Empty(snapshot.Dependencies);
            Assert.Equal(DiagnosticLevel.None, snapshot.GetMaximumVisibleDiagnosticLevelForProvider("foo"));
        }

        [Fact]
        public void CreateEmpty()
        {
            var snapshot = TargetedDependenciesSnapshot.CreateEmpty(_tfm1, _catalogs);

            Assert.Same(_tfm1, snapshot.TargetFramework);
            Assert.Same(_catalogs, snapshot.Catalogs);
            Assert.Equal(DiagnosticLevel.None, snapshot.MaximumVisibleDiagnosticLevel);
            Assert.Empty(snapshot.Dependencies);
            Assert.Equal(DiagnosticLevel.None, snapshot.GetMaximumVisibleDiagnosticLevelForProvider("foo"));
        }

        [Fact]
        public void FromChanges_NoChanges()
        {
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(_tfm1, _catalogs);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes: null,
                _catalogs);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_CatalogChanged()
        {
            var previousCatalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(_tfm1, previousCatalogs);

            var updatedCatalogs = IProjectCatalogSnapshotFactory.Create();

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes: null,
                updatedCatalogs);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(updatedCatalogs, snapshot.Catalogs);
            Assert.Equal(previousSnapshot.Dependencies.Length, snapshot.Dependencies.Length);
            for (int i = 0; i < previousSnapshot.Dependencies.Length; i++)
            {
                Assert.Same(previousSnapshot.Dependencies[i], snapshot.Dependencies[i]);
            }
            Assert.Equal(DiagnosticLevel.None, snapshot.MaximumVisibleDiagnosticLevel);
            Assert.Empty(snapshot.Dependencies);
        }

        [Fact]
        public void FromChanges_AddingToEmpty()
        {
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(_tfm1, _catalogs);

            var resolved = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                OriginalItemSpec = "Dependency1",
                Caption = "Dependency1",
                Resolved = true,
                Flags = ProjectTreeFlags.ResolvedReference,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var unresolved = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency2",
                OriginalItemSpec = "Dependency2",
                Caption = "Dependency2",
                Resolved = false,
                Flags = ProjectTreeFlags.BrokenReference,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var changes = new DependenciesChangesBuilder();
            changes.Added(_tfm1, resolved);
            changes.Added(_tfm1, unresolved);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes.TryBuildChanges()!,
                _catalogs);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(_catalogs, snapshot.Catalogs);
            Assert.Equal(DiagnosticLevel.Warning, snapshot.MaximumVisibleDiagnosticLevel);
            AssertEx.CollectionLength(snapshot.Dependencies, 2);
            Assert.Contains(snapshot.Dependencies, resolved.Matches);
            Assert.Contains(snapshot.Dependencies, unresolved.Matches);
        }

        [Fact]
        public void FromChanges_AddingToNonEmpty()
        {
            var dependency1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                OriginalItemSpec = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependency2 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "dependency2",
                OriginalItemSpec = "Dependency2",
                Caption = "Dependency2",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelNew1 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "newdependency1",
                OriginalItemSpec = "NewDependency1",
                Caption = "NewDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var previousSnapshot = new TargetedDependenciesSnapshot(
                _tfm1,
                _catalogs,
                ImmutableArray.Create<IDependency>(dependency1, dependency2));

            var changes = new DependenciesChangesBuilder();
            changes.Added(_tfm1, dependencyModelNew1);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes.TryBuildChanges()!,
                _catalogs);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(previousSnapshot.Catalogs, snapshot.Catalogs);

            AssertEx.CollectionLength(snapshot.Dependencies, 3);
            Assert.Contains(dependency1, snapshot.Dependencies);
            Assert.Contains(dependency2, snapshot.Dependencies);
            Assert.Contains(snapshot.Dependencies, d => d.Id == dependencyModelNew1.Id);
        }

        [Fact]
        public void FromChanges_DifferentModelIdCapitalisation()
        {

            var dependencyPrevious = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                Resolved = false
            };

            var dependencyModelUpdated = new TestDependencyModel
            {
                ProviderType = "XXX", // changed case
                Id = "DEPENDENCY1",   // changed case
                Resolved = true
            };

            var previousSnapshot = new TargetedDependenciesSnapshot(
                _tfm1,
                _catalogs,
                ImmutableArray.Create<IDependency>(dependencyPrevious));

            var changes = new DependenciesChangesBuilder();
            changes.Added(_tfm1, dependencyModelUpdated);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes.TryBuildChanges()!,
                _catalogs);

            Assert.NotSame(previousSnapshot, snapshot);
            var dependency = Assert.Single(snapshot.Dependencies);
            Assert.Equal("DEPENDENCY1", dependency.Id);
            Assert.Equal("XXX", dependency.ProviderType);
            Assert.True(dependency.Resolved);
        }

        [Fact]
        public void FromChanges_DeduplicatesCaptions_WhenThereIsMatchingDependencies_ShouldUpdateCaptionForAll()
        {
            // Same provider type
            // Same captions
            //   -> Changes caption for both to match alias

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependencyModel
            {
                Id = "id1",
                OriginalItemSpec = "originalItemSpec1",
                ProviderType = providerType,
                Caption = caption
            };

            var otherDependency = new TestDependencyModel
            {
                Id = "id2",
                OriginalItemSpec = "originalItemSpec2",
                ProviderType = providerType,
                Caption = caption
            };

            var changes = new DependenciesChangesBuilder();
            changes.Added(_tfm1, dependency);
            changes.Added(_tfm1, otherDependency);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot: TargetedDependenciesSnapshot.CreateEmpty(_tfm1, _catalogs),
                changes.TryBuildChanges()!,
                _catalogs);

            Assert.Equal(2, snapshot.Dependencies.Length);
            var dep1 = Assert.Single(snapshot.Dependencies, d => d.Id == "id1");
            var dep2 = Assert.Single(snapshot.Dependencies, d => d.Id == "id2");
            Assert.Equal("caption (originalItemSpec1)", dep1.Caption);
            Assert.Equal("caption (originalItemSpec2)", dep2.Caption);
        }

        [Fact]
        public void FromChanges_DeduplicatesCaptions_WhenThereIsMatchingDependencyWithAliasApplied_ShouldUpdateCaptionForCurrentDependency()
        {
            // Same provider type
            // Duplicate caption, though with parenthesized text after one instance
            //   -> Changes caption of non-parenthesized

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependency
            {
                Id = "id1",
                OriginalItemSpec = "originalItemSpec1",
                ProviderType = providerType,
                Caption = $"{caption} (originalItemSpec1)" // caption already includes alias
            };

            var otherDependency = new TestDependencyModel
            {
                Id = "id2",
                ProviderType = providerType,
                OriginalItemSpec = "originalItemSpec2",
                Caption = caption
            };

            var previousSnapshot = new TargetedDependenciesSnapshot(
                _tfm1,
                _catalogs,
                ImmutableArray.Create<IDependency>(dependency));

            var changes = new DependenciesChangesBuilder();
            changes.Added(_tfm1, otherDependency);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes.TryBuildChanges()!,
                _catalogs);

            Assert.Equal(2, snapshot.Dependencies.Length);
            var dep1 = Assert.Single(snapshot.Dependencies, d => d.Id == "id1");
            var dep2 = Assert.Single(snapshot.Dependencies, d => d.Id == "id2");
            Assert.Equal("caption (originalItemSpec1)", dep1.Caption);
            Assert.Equal("caption (originalItemSpec2)", dep2.Caption);
        }

        [Fact]
        public void FromChanges_DeduplicatesCaptions_WhenThereIsMatchingDependency_WithSubstringCaption()
        {
            // TODO test a longer suffix here -- looks like the implementation might not handle it correctly
            
            // Same provider type
            // Duplicate caption prefix
            //   -> No change

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependency
            {
                Id = "id1",
                ProviderType = providerType,
                Caption = caption
            };

            var otherDependencyModel = new TestDependencyModel
            {
                Id = "id2",
                ProviderType = providerType,
                OriginalItemSpec = "dependency2ItemSpec",
                Caption = $"{caption}X" // identical caption prefix
            };

            var previousSnapshot = new TargetedDependenciesSnapshot(
                _tfm1,
                _catalogs,
                ImmutableArray.Create<IDependency>(dependency));

            var changes = new DependenciesChangesBuilder();
            changes.Added(_tfm1, otherDependencyModel);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                previousSnapshot,
                changes.TryBuildChanges()!,
                _catalogs);

            Assert.Equal(2, snapshot.Dependencies.Length);
            var dep1 = Assert.Single(snapshot.Dependencies, d => d.Id == "id1");
            var dep2 = Assert.Single(snapshot.Dependencies, d => d.Id == "id2");
            Assert.Equal("caption", dep1.Caption);
            Assert.Equal("captionX", dep2.Caption);
        }
    }
}
