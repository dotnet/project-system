// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenNotTopLevelOrResolved_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency1",
                topLevel: false);

            var otherDependency = IDependencyFactory.Implement(
                    id: "mydependency2",
                    topLevel:true,
                    resolved:false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();
            
            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: otherDependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenSdk_ShouldFindMatchingPackageAndSetProperties()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.SdkSubTreeNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                resolved:true,
                setPropertiesDependencyIDs: dependencyIDs);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{PackageRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: true,
                    dependencyIDs: dependencyIDs);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenSdkAndPackageUnresolved_ShouldDoNothing()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.SdkSubTreeNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                resolved: true);

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
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenPackage_ShouldFindMatchingSdkAndSetProperties()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.PackageNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                resolved: true,
                dependencyIDs: dependencyIDs);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: true,
                    setPropertiesDependencyIDs: dependencyIDs,
                    equals:true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenPackageAndSdkUnresolved_ShouldDoNothing()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.PackageNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                resolved: true);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenPackageRemoving_ShouldCleanupSdk()
        {
            var dependencyIDs = ImmutableList<string>.Empty;

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                id: "mydependency1id",
                flags: DependencyTreeFlags.PackageNodeFlags,
                name: "mydependency1",
                topLevel: true,
                resolved: true);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    setPropertiesDependencyIDs: dependencyIDs,
                    equals: true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object },
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeRemove(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }
    }
}
