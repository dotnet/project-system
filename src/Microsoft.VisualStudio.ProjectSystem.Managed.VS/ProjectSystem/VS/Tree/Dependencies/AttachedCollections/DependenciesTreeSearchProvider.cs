// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides search results for non-top-level dependency tree nodes of all projects across the solution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delegates solution explorer search for dependencies tree items to <see cref="IDependenciesTreeSearchProvider"/> implementations.
    /// </para>
    /// <para>
    /// Because we only materialize dependencies tree items when needed, we cannot assume here that search results
    /// coincide with existing tree items. Instead, for each match we find we create a new tree item. The tree then
    /// calls back via the 'contained by' relationship to determine parents, repeating the process for all ancestors
    /// until a known item is returned to which the lineage can be attached. For our purposes, these known items are
    /// the <see cref="IVsHierarchyItem"/> instances of top-level project dependencies.
    /// </para>
    /// <para>
    /// Identifying parent hierarchy items requires a bunch of extra context (tree items, hierarchy item manager)
    /// which we don't want to store on every item in order to make it available for relations. All search items and
    /// their ancestors will definitely be expanded in the view, so we can pre-populate their parent collections at
    /// the time they're created. This allows passing the necessary context into the construction process rather than
    /// storing it on items.
    /// </para>
    /// <para>
    /// We must also de-duplicate parent items. If two nodes have a common ancestor, that ancestor must be a single
    /// object, rather than one-per-descendant. This is not required when computing 'contains' relationships.
    /// Again, this means more context specific to the construction of search results.
    /// </para>
    /// <para>
    /// Search results for top-level dependencies occurs via the hierarchy, so those items need not be included here.
    /// </para>
    /// </remarks>
    [AppliesToProject(ProjectCapability.DependenciesTree)]
    [Export(typeof(ISearchProvider))]
    [Name("DependenciesTreeSearchProvider")]
    [VisualStudio.Utilities.Order(Before = "GraphSearchProvider")]
    internal sealed class DependenciesTreeSearchProvider : ISearchProvider
    {
        private readonly ImmutableArray<IDependenciesTreeSearchProvider> _providers;
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly IVsHierarchyItemManager _hierarchyItemManager;
        private readonly IProjectServiceAccessor _projectServiceAccessor;
        private readonly IRelationProvider _relationProvider;

        [ImportingConstructor]
        public DependenciesTreeSearchProvider(
            [ImportMany] IEnumerable<IDependenciesTreeSearchProvider> providers,
            JoinableTaskContext joinableTaskContext,
            IVsHierarchyItemManager hierarchyItemManager,
            IProjectServiceAccessor projectServiceAccessor,
            IRelationProvider relationProvider)
        {
            _providers = providers.ToImmutableArray();
            _joinableTaskContext = joinableTaskContext;
            _hierarchyItemManager = hierarchyItemManager;
            _projectServiceAccessor = projectServiceAccessor;
            _relationProvider = relationProvider;
        }

        public void Search(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
        {
            Requires.NotNull(parameters, nameof(parameters));
            Requires.NotNull(resultAccumulator, nameof(resultAccumulator));

            if (_providers.IsEmpty)
            {
                // No providers registered
                return;
            }

            if (!parameters.Options.SearchExternalItems)
            {
                // Consider the dependencies tree as containing 'external items', allowing the
                // tree to be excluded from search results via this option.
                return;
            }

            using var context = new DependenciesTreeSearchContext(parameters, resultAccumulator);

            _joinableTaskContext.Factory.Run(SearchSolutionAsync);

            Task SearchSolutionAsync()
            {
                // Search projects concurrently
                return Task.WhenAll(_projectServiceAccessor.GetProjectService().LoadedUnconfiguredProjects.Select(SearchProjectAsync));
            }

            async Task SearchProjectAsync(UnconfiguredProject unconfiguredProject)
            {
                IUnconfiguredProjectVsServices? projectVsServices = unconfiguredProject.Services.ExportProvider.GetExportedValue<IUnconfiguredProjectVsServices>();
                IProjectTree? dependenciesNode = projectVsServices?.ProjectTree.CurrentTree?.FindChildWithFlags(DependencyTreeFlags.DependenciesRootNode);

                if (projectVsServices is not null && dependenciesNode is not null)
                {
                    var projectContext = new DependenciesTreeProjectSearchContext(context, unconfiguredProject, dependenciesNode, _hierarchyItemManager, projectVsServices, _relationProvider);

                    // Search providers concurrently
                    await Task.WhenAll(_providers.Select(provider => provider.SearchAsync(projectContext)));
                }
            }
        }
    }
}
