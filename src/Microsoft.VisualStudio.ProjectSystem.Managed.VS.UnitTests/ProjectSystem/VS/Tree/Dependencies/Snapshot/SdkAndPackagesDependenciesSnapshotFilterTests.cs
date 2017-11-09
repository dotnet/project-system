// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class SdkAndPackagesDependenciesSnapshotFilterTests
    {
        [Fact]
        public void WhenNotTopLevelOrResolved_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency1",
                topLevel: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
            }.ToImmutableDictionary().ToBuilder();
            
            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            dependency.VerifyAll();
        }

        [Fact]
        public void WhenSdk_ShouldFindMatchingPackageAndSetProperties()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var flags = DependencyTreeFlags.SdkSubTreeNodeFlags
                               .Union(DependencyTreeFlags.ResolvedFlags)
                                .Except(DependencyTreeFlags.UnresolvedFlags);
            var sdkDependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.SdkSubTreeNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                setPropertiesDependencyIDs: dependencyIDs,
                setPropertiesResolved:true,
                setPropertiesSchemaName: ResolvedSdkReference.SchemaName,
                setPropertiesFlags: flags);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{PackageRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: true,
                    dependencyIDs: dependencyIDs);

            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty.Add(sdkDependency.Object).ToBuilder();
            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { sdkDependency.Object.Id, sdkDependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                mockTargetFramework,
                sdkDependency.Object,
                worldBuilder,
                topLevelBuilder,
                null,
                null,
                out bool filterAnyChanges);

            sdkDependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void WhenSdkAndPackageUnresolved_ShouldDoNothing()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.SdkSubTreeNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{PackageRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                mockTargetFramework,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void WhenPackage_ShouldFindMatchingSdkAndSetProperties()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                id: "mydependency1id",
                flags: DependencyTreeFlags.PackageNodeFlags,
                name: "mydependency1",
                topLevel: true,
                resolved: true,
                dependencyIDs: dependencyIDs);

            var flags = DependencyTreeFlags.PackageNodeFlags
                                           .Union(DependencyTreeFlags.ResolvedFlags)
                                           .Except(DependencyTreeFlags.UnresolvedFlags);
            var sdkDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    flags: DependencyTreeFlags.PackageNodeFlags.Union(DependencyTreeFlags.UnresolvedFlags), // to see if unresolved is fixed
                    setPropertiesResolved:true,
                    setPropertiesDependencyIDs: dependencyIDs,
                    setPropertiesFlags: flags,
                    setPropertiesSchemaName: ResolvedSdkReference.SchemaName,
                    equals:true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { sdkDependency.Object.Id, sdkDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty.Add(sdkDependency.Object).ToBuilder();
            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                mockTargetFramework,
                dependency.Object,
                worldBuilder,
                topLevelBuilder,
                null,
                null,
                out bool filterAnyChanges);

            dependency.VerifyAll();
            sdkDependency.VerifyAll();

            Assert.Equal(topLevelBuilder.First().Id, sdkDependency.Object.Id);
        }

        [Fact]
        public void WhenPackageRemoving_ShouldCleanupSdk()
        {
            var dependencyIDs = ImmutableList<string>.Empty;

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                id: "mydependency1id",
                flags: DependencyTreeFlags.PackageNodeFlags,
                name: "mydependency1",
                topLevel: true,
                resolved: true);

            var flags = DependencyTreeFlags.SdkSubTreeNodeFlags
                                           .Union(DependencyTreeFlags.UnresolvedFlags)
                                           .Except(DependencyTreeFlags.ResolvedFlags);
            var sdkDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    flags: DependencyTreeFlags.SdkSubTreeNodeFlags.Union(DependencyTreeFlags.ResolvedFlags), // to see if resolved is fixed
                    setPropertiesDependencyIDs: dependencyIDs,
                    setPropertiesResolved: false,
                    setPropertiesSchemaName: SdkReference.SchemaName,
                    setPropertiesFlags: flags);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { sdkDependency.Object.Id, sdkDependency.Object },
            }.ToImmutableDictionary().ToBuilder();
            
            // try to have empty top level hash set - no error should happen when removing sdk and readding 
            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty.ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeRemove(
                null,
                mockTargetFramework,
                dependency.Object,
                worldBuilder,
                topLevelBuilder,
                out bool filterAnyChanges);

            dependency.VerifyAll();
            sdkDependency.VerifyAll();

            Assert.Equal(topLevelBuilder.First().Id, sdkDependency.Object.Id);
        }
    }
}
