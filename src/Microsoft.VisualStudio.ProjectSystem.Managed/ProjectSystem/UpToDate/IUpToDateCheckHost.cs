// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Allows the fast up-to-date check to query the host for relevant information.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUpToDateCheckHost
    {
        /// <summary>
        /// Identifies whether design-time builds are available in the host.
        /// </summary>
        /// <remarks>
        /// For example, when Visual Studio runs in "command line" mode, design-time builds do not occur.
        /// </remarks>
        /// <param name="cancellationToken">A token via which the operation may be cancelled.</param>
        /// <returns>A task that, when completed, indicates whether design time builds are available in the host.</returns>
        ValueTask<bool> HasDesignTimeBuildsAsync(CancellationToken cancellationToken);
    }
}
