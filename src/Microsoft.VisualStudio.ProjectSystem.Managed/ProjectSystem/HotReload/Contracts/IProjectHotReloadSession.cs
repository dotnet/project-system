// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

/// <summary>
/// Represents a Hot Reload session within the project system.
/// </summary>
[InternalImplementationOnly]
public interface IProjectHotReloadSession
{
    /// <summary>
    /// Unique id of the session instance.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// A name used to identify the session in diagnostic messages. Not guaranteed to be
    /// unique.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Starts the Hot Reload Session.
    /// </summary>
    /// <param name="cancellationToken">A token indicating if the operation has been cancelled.</param>
    Task StartSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts the Hot Reload Session.
    /// </summary>
    /// <param name="runningUnderDebugger">Unused</param>
    /// <param name="cancellationToken">A token indicating if the operation has been cancelled.</param>
    [Obsolete($"Use the overload that takes a {nameof(CancellationToken)} instead.")]
    Task StartSessionAsync(bool runningUnderDebugger, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the Hot Reload Session.
    /// </summary>
    Task StopSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies any pending changes to the Hot Reload session.
    /// </summary>
    [Obsolete]
    Task ApplyChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates the environment variables in <paramref name="envVars"/> to include
    /// settings needed by a process using Hot Reload.
    /// </summary>
    Task<bool> ApplyLaunchVariablesAsync(IDictionary<string, string> envVars, CancellationToken cancellationToken);
}
