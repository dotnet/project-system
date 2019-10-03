// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class UnresolvedProjectReferenceSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_WhenDependencyNotRecognized_ShouldDoNothing()
        {
            var acceptable = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectDependency
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
                Flags = DependencyTreeFlags.ProjectDependency.Union(DependencyTreeFlags.SharedProjectFlags)
            });

            return;
            static void AssertNoChange(IDependency dependency)
            {
                var aggregateSnapshotProvider = IAggregateDependenciesSnapshotProviderFactory.Create();

                var worldBuilder = new[] { dependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

                var context = new AddDependencyContext(worldBuilder);

                var filter = new UnresolvedProjectReferenceSnapshotFilter(aggregateSnapshotProvider);

                filter.BeforeAddOrUpdate(
                    null!,
                    dependency,
                    null!,
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

            var targetFramework = new TargetFramework(moniker: "tfm1");

            var unresolvedChildDependency = new TestDependency
            {
                Id = "unresolvedChildDependency",
                TopLevel = false,
                Resolved = false,
                Flags = DependencyTreeFlags.ProjectDependency + DependencyTreeFlags.Resolved,
                TargetFramework = targetFramework,
                FullPath = projectPath
            };

            var resolvedDependency = new TestDependency
            {
                Id = "resolvedDependency",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectDependency + DependencyTreeFlags.Resolved,
                TargetFramework = targetFramework,
                FullPath = projectPath,
                DependencyIDs = ImmutableArray.Create(unresolvedChildDependency.Id)
            };

            var targetedSnapshot = TargetedDependenciesSnapshotFactory.ImplementFromDependencies(new [] { resolvedDependency, unresolvedChildDependency });

            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshot(resolvedDependency)).Returns(targetedSnapshot);

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnresolvedProjectReferenceSnapshotFilter(aggregateSnapshotProvider.Object);

            filter.BeforeAddOrUpdate(
                null!,
                resolvedDependency,
                null!,
                null,
                context);

            // Accepts unresolved version
            var acceptedDependency = context.GetResult(filter);
            DependencyAssert.Equal(resolvedDependency.ToUnresolved(ProjectReference.SchemaName), acceptedDependency!);

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
                Flags = DependencyTreeFlags.ProjectDependency.Union(DependencyTreeFlags.Resolved),
                FullPath = projectPath
            };

            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshot(dependency)).Returns((TargetedDependenciesSnapshot?) null);

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnresolvedProjectReferenceSnapshotFilter(aggregateSnapshotProvider.Object);

            filter.BeforeAddOrUpdate(
                null!,
                dependency,
                null!,
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

            var targetFramework = new TargetFramework(moniker: "tfm1");
            var targetedSnapshot = TargetedDependenciesSnapshotFactory.ImplementHasUnresolvedDependency(false);

            var dependency = new TestDependency
            {
                Id = "dependency1",
                TopLevel = true,
                Resolved = true,
                Flags = DependencyTreeFlags.ProjectDependency.Union(DependencyTreeFlags.Resolved),
                FullPath = projectPath,
                TargetFramework = targetFramework
            };

            var aggregateSnapshotProvider = new Mock<IAggregateDependenciesSnapshotProvider>(MockBehavior.Strict);
            aggregateSnapshotProvider.Setup(x => x.GetSnapshot(dependency)).Returns(targetedSnapshot);

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnresolvedProjectReferenceSnapshotFilter(aggregateSnapshotProvider.Object);

            filter.BeforeAddOrUpdate(
                null!,
                dependency,
                null!,
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
