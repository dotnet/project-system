// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

/// <summary>
/// Provides functionality to launch projects using a specified profile during hot reload operations.
/// </summary>
/// <remarks>
/// This interface is used by the Hot Reload system to launch or restart applications 
/// according to a given launch profile. It abstracts the underlying launch mechanism
/// and works in conjunction with <see cref="IProjectBuildManager"/> to support 
/// the full Hot Reload experience.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Extension)]
internal interface IProjectLaunchProvider : IDebugLaunchProvider
{
    /// <summary>
    /// Launches the project using a specified launch profile and debug options.
    /// </summary>
    /// <param name="launchOptions">The debug launch options to use when starting the application.</param>
    /// <param name="profile">The launch profile that defines how the project should be launched.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.
    /// The task result contains a boolean indicating whether the launch was successful (true) or failed (false).
    /// </returns>
    ValueTask<bool> LaunchWithProfileAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, CancellationToken cancellationToken);
}
