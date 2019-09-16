// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class UnresolvedDependenciesSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_WhenUnresolvedAndExistsResolvedInSnapshot_ShouldReturnNull()
        {
            var unresolvedDependency = new TestDependency { Id = "dependency", Resolved = false };
            var resolvedDependency   = new TestDependency { Id = "dependency", Resolved = true  };

            var worldBuilder = new IDependency[] { resolvedDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null!,
                unresolvedDependency,
                null!,
                null,
                context);

            // Dependency rejected
            Assert.Null(context.GetResult(filter));

            // Nothing else changed
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenUnresolvedAndNotExistsResolvedInSnapshot_ShouldReturnDependency()
        {
            var unresolvedDependency = new TestDependency { Id = "dependency", Resolved = false };

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null!,
                unresolvedDependency,
                null!,
                null,
                context);

            // Dependency accepted unchanged
            Assert.Same(unresolvedDependency, context.GetResult(filter));

            // Nothing else changed
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenResolved_ShouldReturnDependency()
        {
            var resolvedDependency = new TestDependency { Id = "dependency", Resolved = true };

            var worldBuilder = ImmutableDictionary<string, IDependency>.Empty.ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                null!,
                resolvedDependency,
                null!,
                null,
                context);

            // Dependency accepted unchanged
            Assert.Same(resolvedDependency, context.GetResult(filter));

            // Nothing else changed
            Assert.False(context.Changed);
        }
    }
}
