﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

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
    /// Activates the pending Hot Reload session and associates it with the specified
    /// process.
    /// </summary>
    Task ActivateSessionAsync(int processId, bool runningUnderDebugger, string projectName);
}
