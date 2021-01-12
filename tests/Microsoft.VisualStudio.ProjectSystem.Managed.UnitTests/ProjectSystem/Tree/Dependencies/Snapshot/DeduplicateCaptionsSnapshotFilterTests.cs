// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class DeduplicateCaptionsSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_NoDuplicate_ShouldNotUpdateCaption()
        {
            // Same provider type
            // Different captions
            //   -> No change

            const string providerType = "provider";

            var dependency = new TestDependency
            {
                Id = "dependency1",
                Caption = "caption1",
                ProviderType = providerType
            };

            var otherDependency = new TestDependency
            {
                Id = "dependency2",
                Caption = "caption2",
                ProviderType = providerType
            };

            var dependencyById = new IDependency[] { dependency, otherDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(dependencyById);

            var filter = new DeduplicateCaptionsSnapshotFilter();

            filter.BeforeAddOrUpdate(
                dependency,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenThereIsMatchingDependencies_ShouldUpdateCaptionForAll()
        {
            // Same provider type
            // Same captions
            //   -> Changes caption for both to match alias

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependency
            {
                Id = "id1",
                OriginalItemSpec = "originalItemSpec1",
                ProviderType = providerType,
                Caption = caption
            };

            var otherDependency = new TestDependency
            {
                ClonePropertiesFrom = dependency, // clone, with changes

                Id = "id2",
                OriginalItemSpec = "originalItemSpec2"
            };

            var dependencyById = new IDependency[] { dependency, otherDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(dependencyById);

            var filter = new DeduplicateCaptionsSnapshotFilter();

            filter.BeforeAddOrUpdate(
                dependency,
                context);

            // The context changed, beyond just the filtered dependency
            Assert.True(context.Changed);

            // The filtered dependency had its caption changed to its alias
            var dependencyAfter = context.GetResult(filter);
            DependencyAssert.Equal(new TestDependency { ClonePropertiesFrom = dependency, Caption = "caption (originalItemSpec1)" }, dependencyAfter!);

            // The other dependency had its caption changed to its alias
            Assert.True(context.TryGetDependency(otherDependency.GetDependencyId(), out IDependency otherDependencyAfter));
            DependencyAssert.Equal(new TestDependency { ClonePropertiesFrom = otherDependency, Caption = "caption (originalItemSpec2)" }, otherDependencyAfter);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenThereIsMatchingDependencyWithAliasApplied_ShouldUpdateCaptionForCurrentDependency()
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
                Caption = caption
            };

            var otherDependency = new TestDependency
            {
                ClonePropertiesFrom = dependency,

                Id = "id2",
                OriginalItemSpec = "originalItemSpec2",
                Caption = $"{caption} (originalItemSpec2)" // caption already includes alias
            };

            var dependencyById = new IDependency[] { dependency, otherDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(dependencyById);

            var filter = new DeduplicateCaptionsSnapshotFilter();

            filter.BeforeAddOrUpdate(
                dependency,
                context);

            // The context was unchanged, beyond the filtered dependency
            Assert.False(context.Changed);

            // The filtered dependency had its caption changed to its alias
            var dependencyAfter = context.GetResult(filter);
            DependencyAssert.Equal(new TestDependency { ClonePropertiesFrom = dependency, Caption = "caption (originalItemSpec1)" }, dependencyAfter!);

            // The other dependency had its caption changed to its alias
            Assert.True(context.TryGetDependency(otherDependency.GetDependencyId(), out IDependency otherDependencyAfter));
            DependencyAssert.Equal(new TestDependency { ClonePropertiesFrom = otherDependency, Caption = "caption (originalItemSpec2)" }, otherDependencyAfter);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenThereIsMatchingDependency_WithSubstringCaption()
        {
            // Same provider type
            // Duplicate caption prefix
            //   -> No change

            const string providerType = "provider";
            const string caption = "caption";

            var dependency = new TestDependency
            {
                Id = "dependency1",
                ProviderType = providerType,
                Caption = caption
            };

            var otherDependency = new TestDependency
            {
                ClonePropertiesFrom = dependency,

                Id = "dependency2",
                OriginalItemSpec = "dependency2ItemSpec",
                Caption = $"{caption}X" // identical caption prefix
            };

            // TODO test a longer suffix here -- looks like the implementation might not handle it correctly

            var dependencyById = new IDependency[] { dependency, otherDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(dependencyById);

            var filter = new DeduplicateCaptionsSnapshotFilter();

            filter.BeforeAddOrUpdate(
                dependency,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }
    }
}
