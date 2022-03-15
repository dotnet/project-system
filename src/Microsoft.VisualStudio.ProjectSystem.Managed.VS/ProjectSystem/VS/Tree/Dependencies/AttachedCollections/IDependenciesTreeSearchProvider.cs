// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Extension point via which dependencies tree providers may implement search across their trees.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Extension, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface IDependenciesTreeSearchProvider
    {
        /// <summary>
        /// Performs an asynchronous search across the dependencies tree provider's internal data for a specific unconfigured project.
        /// </summary>
        /// <remarks>
        /// Implementations should check <see cref="IDependenciesTreeProjectSearchContext.CancellationToken"/> regularly for cancellation.
        /// </remarks>
        /// <param name="searchContext">The context via which search text may be matched and results may be returned.</param>
        Task SearchAsync(IDependenciesTreeProjectSearchContext searchContext);
    }
}
