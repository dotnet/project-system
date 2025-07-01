// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

/// <summary>
/// Interface for managing hot reload related build operations in the project system.
/// </summary>
/// <remarks>
/// This interface is used by the hot reload system to build projects when necessary,
/// such as during Hot Restart operations or when applying updates requires a rebuild.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadBuildManager
{
    /// <summary>
    /// Builds the project and waits for the build to complete.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the build operation.</param>
    /// <returns>
    /// A task that represents the asynchronous build operation.
    /// <c>true</c> if the build was successful; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method is used to build the project to ensure updated code is compiled
    /// before applying hot reload changes or restarting the application.
    /// </remarks>
    Task<bool> BuildProjectAsync(CancellationToken cancellationToken);
}
