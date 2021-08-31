// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Persists fast up-to-date check state across solution lifetimes.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.OneOrZero)]
    internal interface IUpToDateCheckStatePersistence
    {
        /// <summary>
        /// Retrieves the stored up-to-date check state for a given configured project.
        /// </summary>
        /// <param name="projectPath">The full path of the project.</param>
        /// <param name="configurationDimensions">The map of dimension names and values that describes the project configuration.</param>
        /// <returns>The hash and time at which items were last known to have changed (in UTC).</returns>
        Task<(int ItemHash, DateTime ItemsChangedAtUtc)?> RestoreStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions);

        /// <summary>
        /// Stores up-to-date check state for a given configured project.
        /// </summary>
        /// <param name="projectPath">The full path of the project.</param>
        /// <param name="configurationDimensions">The map of dimension names and values that describes the project configuration.</param>
        /// <param name="itemHash">The hash of items to be stored.</param>
        /// <param name="itemsChangedAtUtc">The time at which items were last known to have changed (in UTC).</param>
        void StoreState(string projectPath, IImmutableDictionary<string, string> configurationDimensions, int itemHash, DateTime itemsChangedAtUtc);
    }
}
