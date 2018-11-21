// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class UnsupportedProjectsSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_WhenDependencyNotRecognized_ShouldDoNothing()
        {
            var acceptable = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags
            };

            AssertNoChange(new TestDependency
            {
                ClonePropertiesFrom = acceptable,
                TopLevel = false
            });

            AssertNoChange(new TestDependency
            {
                ClonePropertiesFrom = acceptable,
                Resolved = false
            });

            AssertNoChange(new TestDependency
            {
                ClonePropertiesFrom = acceptable,
                Flags = ProjectTreeFlags.Empty
            });

            AssertNoChange(new TestDependency
            {
                ClonePropertiesFrom = acceptable,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.SharedProjectFlags)
            });

            return;

            void AssertNoChange(IDependency dependency)
            {
                var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Create();
                var targetFrameworkProvider = ITargetFrameworkProviderFactory.Create();

                var worldBuilder = new[] { dependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

                var context = new AddDependencyContext(worldBuilder);

                var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider, targetFrameworkProvider);

                filter.BeforeAddOrUpdate(
                    null,
                    null,
                    dependency,
                    null,
                    null,
                    context);

                // Accepts unchanged dependency
                Assert.Same(dependency, context.GetResult(filter));

                // No other changes made
                Assert.False(context.Changed);
            }
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenProjectSnapshotFoundAndHasUnresolvedDependencies_ShouldMakeUnresolved()
        {
            const string projectPath = @"c:\project\project.csproj";

            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: true);
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>
            {
                { targetFramework, targetedSnapshot }
            };
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets);
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: snapshot);
            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshotProvider(projectPath)).Returns(snapshotProvider);

            var targetFrameworkProvider = ITargetFrameworkProviderFactory.Implement(getNearestFramework: targetFramework);

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                TargetFramework = targetFramework,
                FullPath = projectPath
            };

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object, targetFrameworkProvider);

            filter.BeforeAddOrUpdate(
                null,
                null,
                dependency,
                null,
                null,
                context);

            // Accepts unresolved version
            var acceptedDependency = context.GetResult(filter);
            acceptedDependency.AssertEqualTo(
                dependency.ToUnresolved(ProjectReference.SchemaName));

            // No other changes made
            Assert.False(context.Changed);

            aggregateSnapshotProvider.VerifyAll();
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenProjectSnapshotNotFound_ShouldDoNothing()
        {
            const string projectPath = @"c:\project\project.csproj";

            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: null);
            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshotProvider(projectPath)).Returns(snapshotProvider);

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                FullPath = projectPath
            };

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object, targetFrameworkProvider: null);

            filter.BeforeAddOrUpdate(
                null,
                null,
                dependency,
                null,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);

            aggregateSnapshotProvider.VerifyAll();
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenProjectSnapshotFoundAndTargetFrameworkNull_ShouldDoNothing()
        {
            const string projectPath = @"c:\project\project.csproj";

            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: true);
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>
            {
                { targetFramework, targetedSnapshot }
            };
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets);
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: snapshot);
            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshotProvider(projectPath)).Returns(snapshotProvider);
            var targetFrameworkProvider = ITargetFrameworkProviderFactory.Implement(getNearestFramework: null);

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                FullPath = projectPath,
                TargetFramework = targetFramework
            };

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object, targetFrameworkProvider);

            filter.BeforeAddOrUpdate(
                null,
                null,
                dependency,
                null,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);

            aggregateSnapshotProvider.VerifyAll();
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenProjectSnapshotFoundAndNoUnresolvedDependencies_ShouldDoNothing()
        {
            const string projectPath = @"c:\project\project.csproj";

            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: false);
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>
            {
                { targetFramework, targetedSnapshot }
            };
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets);
            var snapshotProvider = IDependenciesSnapshotProviderFactory.Implement(currentSnapshot: snapshot);
            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshotProvider(projectPath)).Returns(snapshotProvider);
            var targetFrameworkProvider = ITargetFrameworkProviderFactory.Implement(getNearestFramework: targetFramework);

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                FullPath = projectPath,
                TargetFramework = targetFramework
            };

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object, targetFrameworkProvider);

            filter.BeforeAddOrUpdate(
                null,
                null,
                dependency,
                null,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);

            aggregateSnapshotProvider.VerifyAll();
        }
    }
}
