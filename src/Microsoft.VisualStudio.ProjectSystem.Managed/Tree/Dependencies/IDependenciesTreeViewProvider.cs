// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
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

        /// <summary>
        /// Finds node by path in current dependencies view hierarchy.
        /// </summary>
        /// <param name="root">Node where we start searching</param>
        /// <param name="path">Path to find</param>
        IProjectTree? FindByPath(IProjectTree? root, string path);
    }
}
