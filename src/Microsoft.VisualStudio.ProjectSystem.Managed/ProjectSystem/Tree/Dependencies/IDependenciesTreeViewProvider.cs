// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Abstraction that helps to build different views for Dependencies node contents.
    /// Having multiple views implementations we could have some context commands switching
    /// between different views.
    /// View is responsible for building nodes hierarchy based on given dependencies snapshot.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IDependenciesTreeViewProvider
    {
        /// <summary>
        /// Builds the "Dependencies" node <see cref="IProjectTree"/> for the given <paramref name="snapshot"/> based on the previous <paramref name="dependenciesTree"/>.
        /// Implementations should nest all top-level dependencies beneath this node, potentially grouped by target framework, dependency type and so forth.
        /// </summary>
        /// <param name="dependenciesTree">The previous dependencies tree, to which the updated <paramref name="snapshot"/> should be applied.</param>
        /// <param name="snapshot">The current dependencies snapshot to apply to the tree.</param>
        /// <param name="cancellationToken">Supports cancellation of this operation.</param>
        /// <returns>An updated "Dependencies" node.</returns>
        Task<IProjectTree> BuildTreeAsync(
            IProjectTree dependenciesTree,
            DependenciesSnapshot snapshot,
            CancellationToken cancellationToken = default);
    }
}
