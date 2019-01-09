using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public abstract class DependenciesSnapshotFilterTestsBase
    {
        private protected abstract IDependenciesSnapshotFilter CreateFilter();

        private protected void VerifyUnchangedOnAdd(IDependency dependency, IImmutableSet<string> projectItemSpecs = null)
        {
            var worldBuilder = new[] { dependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = CreateFilter();

            filter.BeforeAddOrUpdate(
                null,
                null,
                dependency,
                null,
                projectItemSpecs,
                context);

            // Accepts unchanged dependency
            Assert.Same(dependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }
    }
}
