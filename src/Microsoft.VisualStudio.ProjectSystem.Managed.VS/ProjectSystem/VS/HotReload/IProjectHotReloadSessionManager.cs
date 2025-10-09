// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

/// <summary>
/// Tracks and manages the pending and active Hot Reload sessions for the project.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IProjectHotReloadSessionManager
{
    /// <summary>
    /// Creates a pending Hot Reload session for the project as possible, and updates
    /// the given <paramref name="environmentVariables"/> accordingly.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the session was created; <see langword="false"/>
    /// otherwise.
    /// </returns>
    Task<bool> TryCreatePendingSessionAsync(
        IProjectHotReloadLaunchProvider launchProvider,
        IDictionary<string, string> environmentVariables,
        DebugLaunchOptions launchOptions,
        ILaunchProfile launchProfile);

    /// <summary>
    /// Activates the pending Hot Reload session and associates it with the specified process.
    /// The <paramref name="launchedProcess"/> and/or <paramref name="vsDebugTargetProcessInfo"/> provide the launch information of the process.
    /// When the session is terminated, the <paramref name="launchedProcess"/> will be used to terminate the process if it is not null.
    /// Otherwise, the PID from <paramref name="vsDebugTargetProcessInfo"/> will be used to terminate the process.
    /// </summary>
    /// <param name="launchedProcess">if not null, it will be used to terminate the process.</param>
    /// <param name="vsDebugTargetProcessInfo">The process information of the launched process.</param>
    Task ActivateSessionAsync(IVsLaunchedProcess? launchedProcess, VsDebugTargetProcessInfo vsDebugTargetProcessInfo);
}
