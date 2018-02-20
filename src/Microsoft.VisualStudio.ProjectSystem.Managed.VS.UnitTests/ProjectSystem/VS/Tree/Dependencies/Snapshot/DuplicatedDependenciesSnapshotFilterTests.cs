// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class DuplicatedDependenciesSnapshotFilterTests
    {
        [Fact]
        public void WhenThereNoMatchingDependencies_ShouldNotUpdateCaption()
        {
            const string caption = "MyCaption";
            var dependency = IDependencyFactory.Implement(
                providerType:"myprovider", 
                id:"mydependency1", 
                caption:caption);

            var otherDependency = IDependencyFactory.Implement(
                    providerType: "myprovider",
                    id: "mydependency2",
                    caption: "otherCaption");

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();
            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty
                                                               .Add(dependency.Object)
                                                               .Add(otherDependency.Object)
                                                               .ToBuilder();
            
            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                topLevelBuilder,
                null,
                null,
                out bool filterAnyChanges);

            Assert.False(worldBuilder.ContainsKey(otherDependency.Object.Id));

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void WhenThereIsMatchingDependencies_ShouldUpdateCaptionForAll()
        {
            const string caption = "MyCaption";
            var dependency = IDependencyFactory.Implement(
                providerType: "myprovider",
                id: "mydependency1",
                caption: caption,
                alias: "mydependency1 (mydependency1ItemSpec)",
                setPropertiesCaption: "mydependency1 (mydependency1ItemSpec)");

            var otherDependency = IDependencyFactory.Implement(
                    providerType: "myprovider",
                    id: "mydependency2",
                    caption: caption,
                    alias: "mydependency2 (mydependency2ItemSpec)",
                    setPropertiesCaption: "mydependency2 (mydependency2ItemSpec)",
                    equals:true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();
            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty
                                                               .Add(dependency.Object)
                                                               .Add(otherDependency.Object)
                                                               .ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                topLevelBuilder,
                null,
                null,
                out bool filterAnyChanges);

            Assert.True(worldBuilder.ContainsKey(otherDependency.Object.Id));

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void WhenThereIsMatchingDependencyWithAliasApplied_ShouldUpdateCaptionForCurrentDependency()
        {
            const string caption = "MyCaption";
            var dependency = IDependencyFactory.Implement(
                providerType: "myprovider",
                id: "mydependency1",
                caption: caption,
                alias: "mydependency1 (mydependency1ItemSpec)",
                setPropertiesCaption: "mydependency1 (mydependency1ItemSpec)");

            var otherDependency = IDependencyFactory.Implement(
                   originalItemSpec: "mydependency2ItemSpec",
                    providerType: "myprovider",
                    id: "mydependency2",
                    caption: $"{caption} (mydependency2ItemSpec)");

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();
            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty
                                                               .Add(dependency.Object)
                                                               .Add(otherDependency.Object)
                                                               .ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                topLevelBuilder,
                null,
                null,
                out bool filterAnyChanges);

            Assert.False(worldBuilder.ContainsKey(otherDependency.Object.Id));

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void WhenThereIsMatchingDependency_WithSubstringCaption()
        {
            const string caption = "MyCaption";
            var dependency = IDependencyFactory.Implement(
                providerType: "myprovider",
                id: "mydependency1",
                caption: caption);

            var otherDependency = IDependencyFactory.Implement(
                    providerType: "myprovider",
                    id: "mydependency2",
                    caption: caption + "X");

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();
            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty
                                                               .Add(dependency.Object)
                                                               .Add(otherDependency.Object)
                                                               .ToBuilder();

            var filter = new DuplicatedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                topLevelBuilder,
                null,
                null,
                out bool filterAnyChanges);

            Assert.False(worldBuilder.ContainsKey(otherDependency.Object.Id));

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }
    }
}
