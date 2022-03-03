// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class UnresolvedDependenciesSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_WhenUnresolvedAndExistsResolvedInSnapshot_ShouldReturnNull()
        {
            var unresolvedDependency = new TestDependency { Id = "dependency", Resolved = false };
            var resolvedDependency   = new TestDependency { Id = "dependency", Resolved = true  };

            var dependencyById = new Dictionary<DependencyId, IDependency>
            {
                { resolvedDependency.GetDependencyId(), resolvedDependency }
            };

            var context = new AddDependencyContext(dependencyById);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                unresolvedDependency,
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

            var dependencyById = new Dictionary<DependencyId, IDependency>();

            var context = new AddDependencyContext(dependencyById);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                unresolvedDependency,
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

            var dependencyById = new Dictionary<DependencyId, IDependency>();

            var context = new AddDependencyContext(dependencyById);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                resolvedDependency,
                context);

            // Dependency accepted unchanged
            Assert.Same(resolvedDependency, context.GetResult(filter));

            // Nothing else changed
            Assert.False(context.Changed);
        }
    }
}
