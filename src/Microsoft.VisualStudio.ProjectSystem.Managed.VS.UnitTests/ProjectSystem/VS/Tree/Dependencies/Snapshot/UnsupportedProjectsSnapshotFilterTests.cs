// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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

                var worldBuilder = new[] { dependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

                var context = new AddDependencyContext(worldBuilder);

                var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider);

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

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                TargetFramework = targetFramework,
                FullPath = projectPath
            };

            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(hasUnresolvedDependency: true);

            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshot(dependency)).Returns(targetedSnapshot);

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object);

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

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                FullPath = projectPath
            };

            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshot(dependency)).Returns((ITargetedDependenciesSnapshot) null);

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object);

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

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectNodeFlags.Union(DependencyTreeFlags.ResolvedFlags),
                FullPath = projectPath,
                TargetFramework = targetFramework
            };

            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshot(dependency)).Returns(targetedSnapshot);

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnsupportedProjectsSnapshotFilter(aggregateSnapshotProvider.Object);

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
