// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class ImplicitTopLevelDependenciesSnapshotFilterTests
    {
        [Fact]
        public void ImplicitTopLevelDependenciesSnapshotFilter_WhenNotTopLevel_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                topLevel: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.Equal(dependency.Object.Id, resultDependency.Id);
            Assert.False(filterAnyChanges);

            dependency.VerifyAll();
        }

        [Fact]
        public void ImplicitTopLevelDependenciesSnapshotFilter_WhenImplicitAlready_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                topLevel: true,
                isImplicit: true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.Equal(dependency.Object.Id, resultDependency.Id);
            Assert.False(filterAnyChanges);

            dependency.VerifyAll();
        }

        [Fact]
        public void ImplicitTopLevelDependenciesSnapshotFilter_WhenUnresolved_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                topLevel: true,
                isImplicit: false,
                resolved: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();
           
            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.Equal(dependency.Object.Id, resultDependency.Id);
            Assert.False(filterAnyChanges);

            dependency.VerifyAll();
        }

        [Fact]
        public void ImplicitTopLevelDependenciesSnapshotFilter_WhenNotGenericDependency_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                topLevel: true,
                isImplicit: false,
                resolved: true,
                flags: DependencyTreeFlags.SubTreeRootNodeFlags);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.Equal(dependency.Object.Id, resultDependency.Id);
            Assert.False(filterAnyChanges);

            dependency.VerifyAll();
        }

        [Fact]
        public void ImplicitTopLevelDependenciesSnapshotFilter_WhenCanApplyImplicitProjectContainsItem_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                topLevel: true,
                isImplicit: false,
                resolved: true,
                flags: DependencyTreeFlags.GenericDependencyFlags,
                originalItemSpec: "myprojectitem");

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                new HashSet<string>(new[] { "myprojectitem" }),
                out bool filterAnyChanges);

            Assert.Equal(dependency.Object.Id, resultDependency.Id);
            Assert.False(filterAnyChanges);

            dependency.VerifyAll();
        }

        [Fact]
        public void ImplicitTopLevelDependenciesSnapshotFilter_WhenNeedToApplyImplicit_ShouldSetProperties()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                providerType: "myProvider",
                topLevel: true,
                isImplicit: false,
                resolved: true,
                flags: DependencyTreeFlags.GenericDependencyFlags,
                originalItemSpec: "myprojectitem",
                setPropertiesImplicit: true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var subTreeProvider = IProjectDependenciesSubTreeProviderFactory.ImplementInternal(
                providerType: "myProvider",
                icon: KnownMonikers.Abbreviation);

            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();
            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>
                {
                    { subTreeProvider.ProviderType, subTreeProvider }
                },
                new HashSet<string>(),
                out bool filterAnyChanges);

            Assert.Equal(dependency.Object.Id, resultDependency.Id);
            Assert.True(filterAnyChanges);

            dependency.VerifyAll();
        }
    }
}
