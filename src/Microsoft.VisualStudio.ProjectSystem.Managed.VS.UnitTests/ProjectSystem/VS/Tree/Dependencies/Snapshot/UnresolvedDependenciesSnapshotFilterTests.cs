// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class UnresolvedDependenciesSnapshotFilterTests
    {
        [Fact]
        public void WhenUnresolvedAndExistsResolvedInSnapshot_ShouldReturnNull()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                resolved:false);

            var otherDependency = IDependencyFactory.Implement(                    
                    id: "mydependency2");

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();
           
            var filter = new UnresolvedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.Null(resultDependency);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void WhenUnresolvedAndNotExistsResolvedInSnapshot_ShouldReturnDependency()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                resolved: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
            }.ToImmutableDictionary().ToBuilder();

            var filter = new UnresolvedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                worldBuilder,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.NotNull(resultDependency);
            Assert.Equal("mydependency2", resultDependency.Id);

            dependency.VerifyAll();
        }

        [Fact]
        public void WhenResolved_ShouldReturnDependency()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency2",
                resolved: true);

            var filter = new UnresolvedDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                null,
                null,
                dependency.Object,
                null,
                null,
                null,
                null,
                out bool filterAnyChanges);

            Assert.NotNull(resultDependency);
            Assert.Equal("mydependency2", resultDependency.Id);

            dependency.VerifyAll();
        }
    }
}
