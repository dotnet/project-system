// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ProjectTreeServiceExtensions
    {
        // The loading tree's generation version is -1, and the physical tree's version starts from 0.
        private const long MinimumPhysicalTreeGeneration = 0L;

        /// <summary>
        ///     Returns a task that will complete when a tree that includes data that meets the
        ///     specified requirements is published, and whose result will be the data about
        ///     that tree.
        /// </summary>
        /// <param name="treeService">
        ///     The <see cref="IProjectTreeService"/> tow 
        /// </param>
        /// <param name="minimumRequiredDataSourceVersions">
        ///     The minimum required versions of various data sources that may be included in
        ///     the tree.
        /// </param>
        /// <param name="blockDuringLoadingTree">
        ///     <see langword="true"/> if the caller wants to block until the initial (real) project 
        ///     tree (not the loading tree) is published.
        /// </param>
        /// <param name="cancellationToken">
        ///      A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="treeService"/> is <see langword="null"/>.
        /// </exception>
        public static Task<IProjectTreeServiceState> PublishTreeAsync(this IProjectTreeService treeService, IImmutableDictionary<NamedIdentity, IProjectVersionRequirement> minimumRequiredDataSourceVersions, bool blockDuringLoadingTree, CancellationToken cancellationToken = default(CancellationToken))
        {
            Requires.NotNull(treeService, nameof(treeService));

            if (blockDuringLoadingTree)
            {
                minimumRequiredDataSourceVersions = minimumRequiredDataSourceVersions.SetItem(
                    ProjectTreeDataSources.BaseTreeGeneration,
                    new ProjectVersionRequirement(MinimumPhysicalTreeGeneration, allowMissingData: false));
            }


            return treeService.PublishTreeAsync(minimumRequiredDataSourceVersions);
        }
    }
}
