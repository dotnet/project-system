// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class DuplicatedDependenciesSnapshotFilterTests
    {
        [Fact]
        public void BeforeAdd_NoDuplicate_ShouldNotUpdateCaption()
        {
            // Both top level
            // Same provider type
            // Different captions
            //   -> No change

            const string providerType = "myprovider";

            var dependency = IDependencyFactory.Implement(
                providerType: providerType,
                id: "mydependency1",
                caption: "MyCaption",
                topLevel: true);

            var otherDependency = IDependencyFactory.Implement(
                    providerType: providerType,
                    id: "mydependency2",
                    caption: "otherCaption",
                    topLevel: true);

            var worldBuilder = new[] { dependency.Object, otherDependency.Object }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                out bool filterAnyChanges);

            dependency.VerifyAll();
            otherDependency.VerifyAll();

            Assert.False(filterAnyChanges);
            Assert.Same(dependency.Object, resultDependency);
            Assert.Same(dependency.Object, worldBuilder[dependency.Object.Id]);
            Assert.Same(otherDependency.Object, worldBuilder[otherDependency.Object.Id]);
        }

        [Fact]
        public void BeforeAdd_WhenThereIsMatchingDependencies_ShouldUpdateCaptionForAll()
        {
            // Both top level
            // Same provider type
            // Same captions
            //   -> Changes caption for both to match alias

            const string providerType = "myprovider";
            const string caption = "MyCaption";

            var dependency = IDependencyFactory.Implement(
                providerType: providerType,
                id: "mydependency1",
                caption: caption,
                alias: "mydependency1 (mydependency1ItemSpec)",
                setPropertiesCaption: "mydependency1 (mydependency1ItemSpec)", // should set caption to its alias
                topLevel: true);

            var otherDependency = IDependencyFactory.Implement(
                    providerType: providerType,
                    id: "mydependency2",
                    caption: caption,
                    alias: "mydependency2 (mydependency2ItemSpec)",
                    setPropertiesCaption: "mydependency2 (mydependency2ItemSpec)", // should set caption to its alias
                    equals: true,
                    topLevel: true);

            var worldBuilder = new[] { dependency.Object, otherDependency.Object }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                out bool filterAnyChanges);

            Assert.True(worldBuilder.ContainsKey(otherDependency.Object.Id));

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void BeforeAdd_WhenThereIsMatchingDependencyWithAliasApplied_ShouldUpdateCaptionForCurrentDependency()
        {
            // Both top level
            // Same provider type
            // Duplicate caption, though with parenthesized text after one instance
            //   -> Changes caption of non-parenthesized

            const string providerType = "myprovider";
            const string caption = "MyCaption";

            var dependencyReplacement = IDependencyFactory.Create();

            var dependency = IDependencyFactory.Implement(
                providerType: providerType,
                id: "mydependency1",
                caption: caption,
                alias: "mydependency1 (mydependency1ItemSpec)",
                setPropertiesCaption: "mydependency1 (mydependency1ItemSpec)", // should set caption to its alias
                setPropertiesReturn: dependencyReplacement,
                topLevel: true);

            var otherDependency = IDependencyFactory.Implement(
                   originalItemSpec: "mydependency2ItemSpec",
                    providerType: providerType,
                    id: "mydependency2",
                    caption: $"{caption} (mydependency2ItemSpec)", // caption already includes alias
                    topLevel: true);

            var worldBuilder = new[] { dependency.Object, otherDependency.Object }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                out bool filterAnyChanges);

            dependency.VerifyAll();
            otherDependency.VerifyAll();

            Assert.True(filterAnyChanges);
            Assert.Same(dependencyReplacement, resultDependency);
            Assert.Same(dependency.Object, worldBuilder[dependency.Object.Id]); // doesn't update worldBuilder
            Assert.Same(otherDependency.Object, worldBuilder[otherDependency.Object.Id]);
        }

        [Fact]
        public void BeforeAdd_WhenThereIsMatchingDependency_WithSubstringCaption()
        {
            // Both top level
            // Same provider type
            // Duplicate caption, though with parenthesized text after one instance
            //   -> Changes caption of non-parenthesized

            const string providerType = "myprovider";
            const string caption = "MyCaption";

            var dependency = IDependencyFactory.Implement(
                providerType: providerType,
                id: "mydependency1",
                caption: caption,
                topLevel: true);

            var otherDependency = IDependencyFactory.Implement(
                    providerType: providerType,
                    id: "mydependency2",
                    caption: caption + "X",
                    topLevel: true);

            var worldBuilder = new[] { dependency.Object, otherDependency.Object }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                out bool filterAnyChanges);

            dependency.VerifyAll();
            otherDependency.VerifyAll();

            Assert.False(filterAnyChanges);
            Assert.Same(dependency.Object, resultDependency);
        }
    }
}
