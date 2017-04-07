// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class UnsupportedProjectsSnapshotFilterTests
    {
        [Fact]
        public void UnsupportedProjectsSnapshotFilter_WhenDependencyNotRecognized_ShouldDoNothing()
        {
            var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Create();
            var targetFRameworkProvider = ITargetFrameworkProviderFactory.Create();

            var dependency = IDependencyFactory.Implement(
                id: "mydependency1",
                topLevel: false);

            var topLevelDependency = IDependencyFactory.Implement(
                    id: "mydependency2",
                    topLevel:true,
                    resolved:false);

            var topLevelResolvedDependency = IDependencyFactory.Implement(
                    id: "mydependency3",
                    topLevel: true,
                    resolved: true,
                    flags:ProjectTreeFlags.Empty);

            var topLevelResolvedSharedProjectDependency = IDependencyFactory.Implement(
                    id: "mydependency4",
                    topLevel: true,
                    resolved: true,
                    flags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.SharedProjectFlags));

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { topLevelDependency.Object.Id, topLevelDependency.Object },
                { topLevelResolvedDependency.Object.Id, topLevelResolvedDependency.Object },
                { topLevelResolvedSharedProjectDependency.Object.Id, topLevelResolvedSharedProjectDependency.Object },

            }.ToImmutableDictionary().ToBuilder();
            
            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider, targetFRameworkProvider);

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: topLevelDependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: topLevelResolvedDependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: topLevelResolvedSharedProjectDependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            topLevelDependency.VerifyAll();
        }

        [Fact]
        public void UnsupportedProjectsSnapshotFilter_WhenProjectSnapshotFoundAndHasUnresolvedDependencies_ShouldMakeUnresolved()
        {
            // Arrange 
            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: true);
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>
            {
                { targetFramework, targetedSnapshot }
            };
            var snapshot = IDependenciesSnapshotFactory.Implement(targets:targets);
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: snapshot);
            var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Implement(getSnapshotProvider: snapshotProvider);
            var targetFrameworkProvider = ITargetFrameworkProviderFactory.Implement(getNearestFramework: targetFramework);

            var dependency = IDependencyFactory.Implement(
                    topLevel: true,
                    resolved: true,
                    flags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                    originalItemSpec:@"c:\myproject2\project.csproj",
                    snapshot: targetedSnapshot,
                    setPropertiesResolved:false,
                    setPropertiesFlags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.UnresolvedFlags));

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider, targetFrameworkProvider);

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: null,
                topLevelBuilder: null);            

            dependency.VerifyAll();
        }

        [Fact]
        public void UnsupportedProjectsSnapshotFilter_WhenProjectSnapshotNotFound_ShouldMakeUnresolved()
        {
            // Arrange 
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: null);
            var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Implement(getSnapshotProvider: snapshotProvider);

            var dependency = IDependencyFactory.Implement(
                    topLevel: true,
                    resolved: true,
                    flags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                    originalItemSpec: @"c:\myproject2\project.csproj",
                    setPropertiesResolved: false,
                    setPropertiesFlags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.UnresolvedFlags));

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider, null);

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: null,
                topLevelBuilder: null);

            dependency.VerifyAll();
        }

        [Fact]
        public void UnsupportedProjectsSnapshotFilter_WhenProjectSnapshotFoundAndTargetFrameworkNull_ShouldMakeUnresolved()
        {
            // Arrange 
            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: true);
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>
            {
                { targetFramework, targetedSnapshot }
            };
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets);
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: snapshot);
            var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Implement(getSnapshotProvider: snapshotProvider);
            var targetFrameworkProvider = ITargetFrameworkProviderFactory.Implement(getNearestFramework: null);

            var dependency = IDependencyFactory.Implement(
                    topLevel: true,
                    resolved: true,
                    flags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                    originalItemSpec: @"c:\myproject2\project.csproj",
                    snapshot: targetedSnapshot,
                    setPropertiesResolved: false,
                    setPropertiesFlags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.UnresolvedFlags));

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider, targetFrameworkProvider);

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: null,
                topLevelBuilder: null);

            dependency.VerifyAll();
        }

        [Fact]
        public void UnsupportedProjectsSnapshotFilter_WhenProjectSnapshotFoundAndNoUnresolvedDependencies_ShouldDoNothing()
        {
            // Arrange 
            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: false);
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>
            {
                { targetFramework, targetedSnapshot }
            };
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets);
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: snapshot);
            var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Implement(getSnapshotProvider:snapshotProvider);
            var targetFrameworkProvider = ITargetFrameworkProviderFactory.Implement(getNearestFramework: targetFramework);

            var dependency = IDependencyFactory.Implement(
                    topLevel: true,
                    resolved: true,
                    flags: DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                    originalItemSpec: @"c:\myproject2\project.csproj",
                    snapshot:targetedSnapshot);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider, targetFrameworkProvider);

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: null,
                topLevelBuilder: null);

            dependency.VerifyAll();
        }
    }
}
