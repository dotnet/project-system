// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public abstract class DependenciesSnapshotFilterTestsBase
    {
        private protected abstract IDependenciesSnapshotFilter CreateFilter();

        private protected void VerifyUnchangedOnAdd(IDependency dependency, IImmutableSet<string>? projectItemSpecs = null)
        {
            var dependencyById = new Dictionary<string, IDependency>
            {
                { dependency.Id, dependency }
            };

            var context = new AddDependencyContext(dependencyById);

            var filter = CreateFilter();

            filter.BeforeAddOrUpdate(
                dependency,
                null!,
                projectItemSpecs,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }
    }
}
