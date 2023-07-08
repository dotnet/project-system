// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

/// <summary>
/// Mef Export that can be used to be notified when the hot reload state changes for a project
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadNotificationService
{
    /// <summary>
    /// Subscribe to this event to be notified of changes to a projects hot reload state. 
    /// </summary>
    event AsyncEventHandler<bool> HotReloadStateChangedAsync;

    /// <summary>
    /// The current state of hot reload for the project
    /// </summary>
    bool IsProjectInHotReload{ get; }

    /// <summary>
    /// Called by project systems when it enters or exits a hot reload session.
    /// </summary>
    Task SetHotReloadStateAsync(bool isInHotReload);
}
