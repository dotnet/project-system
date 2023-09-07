// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Aggregates the set of components required by projects across the solution, allowing in-product
/// acquisition to direct the user towards a convenient installation experience for any missing components.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private)]
internal interface ISetupComponentRegistrationService
{
    /// <summary>
    /// Register a project to be tracked for components to be installed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that unregisters the project when disposed.</returns>
    /// <param name="projectGuid">Identifies the project providing the data.</param>
    /// <param name="cancellationToken">Signals a loss of interest in the result of this operation.</param>
    Task<IDisposable> RegisterProjectAsync(Guid projectGuid, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the latest component snapshot for an unconfigured project.
    /// </summary>
    /// <remarks>
    /// The project must first be registered via <see cref="RegisterProjectAsync" />.
    /// </remarks>
    /// <param name="projectGuid">Identifies the project providing the data.</param>
    /// <param name="snapshot">The set of components required by the calling project.</param>
    void SetProjectComponentSnapshot(Guid projectGuid, UnconfiguredSetupComponentSnapshot snapshot);
}
