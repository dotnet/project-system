// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    /// <summary>
    /// Provides a mechanism to apply custom updates to the running hot reload sessions. Assumes all updates can be applied via the 
    /// IDeltaApplier created for this session
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.System, Cardinality = Composition.ImportCardinality.ExactlyOne)]
    public interface IProjectHotReloadUpdateApplier
    {
        /// <summary>
        /// Returns true if there is at least one hot reload session active in the project
        /// </summary>
        bool HasActiveHotReloadSessions { get; }

        /// <summary>
        /// Called to apply a custom update via the IDeltaApplier. The applyFunction will be called once for every active session, and passed the IDeltaApplier
        /// for that session. In the case of multiple sessions, updates will stop being applied on the first call to an applyFunction that throws an exception, and 
        /// that exception propagates back to the caller.
        /// </summary>
        Task ApplyHotReloadUpdateAsync(Func<IDeltaApplier, CancellationToken, Task> applyFunction, CancellationToken cancelToken);
    }
}
