// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

/// <summary>
/// Provides functionality for launching or restarting applications during Hot Reload operations.
/// </summary>
/// <remarks>
/// This interface is used by the Hot Reload system to launch applications with specific profiles
/// and debug options, typically after changes have been applied that require application restart.
/// It operates at the ConfiguredProject scope to ensure the correct configuration is used for launch.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadLaunchProvider
{
    /// <summary>
    /// Launches or relaunches a project with the specified launch profile and debug options.
    /// </summary>
    /// <param name="launchOptions">
    /// The debug launch options to use when launching the project.
    /// These options control aspects like whether to debug the process, 
    /// whether to use an integrated console, and other debugging behaviors.
    /// </param>
    /// <param name="profile">
    /// The launch profile to use, containing settings such as command name,
    /// executable path, working directory, and environment variables.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous launch operation.</returns>
    /// <remarks>
    /// This method is primarily used by the Hot Reload system to restart applications
    /// when changes require a full restart, such as when certain types of code changes 
    /// cannot be applied using in-place Hot Reload techniques.
    /// </remarks>
    Task LaunchWithProfileAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, CancellationToken cancellationToken);
}
