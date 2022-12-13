// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Creates instances of <see cref="Workspace"/> for "slices" of an unconfigured project.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IWorkspaceFactory
{
    /// <summary>
    /// Creates an initialized workspace for the given project slice. The workspace will update
    /// itself until disposed.
    /// </summary>
    /// <param name="source">A special project subscription source for data from a single slice.</param>
    /// <param name="slice">The slice this workspace represents.</param>
    /// <param name="joinableTaskCollection">A collection for joinable tasks.</param>
    /// <param name="joinableTaskFactory">A factory for joinable tasks.</param>
    /// <param name="projectGuid">The project's GUID.</param>
    /// <param name="cancellationToken">Signals a loss of interest in the result of this operation.</param>
    /// <returns>The created workspace.</returns>
    Workspace Create(
        IActiveConfigurationSubscriptionSource source,
        ProjectConfigurationSlice slice,
        JoinableTaskCollection joinableTaskCollection,
        JoinableTaskFactory joinableTaskFactory,
        Guid projectGuid,
        CancellationToken cancellationToken);
}
