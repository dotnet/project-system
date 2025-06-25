// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

/// <summary>
/// Provides functionality to build a project during hot reload operations.
/// </summary>
/// <remarks>
/// This interface is used by the Hot Reload system to build projects before applying
/// changes or restarting applications. It abstracts the underlying build mechanism
/// and allows for proper integration with Visual Studio's build system.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IProjectBuildManager
{
    /// <summary>
    /// Builds the project for a given target framework and waits for the build to complete.
    /// </summary>
    /// <param name="targetFramework">The target framework to build for, or null to use the current active framework.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The task result contains a boolean indicating whether the build was successful (true) or failed (false).
    /// </returns>
    /// <remarks>
    /// This method is primarily used during Hot Reload operations to ensure the project is built
    /// before applying changes or restarting the application.
    /// </remarks>
    ValueTask<bool> BuildProjectAsync(string? targetFramework, CancellationToken cancellationToken);
}
