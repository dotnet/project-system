// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    public interface IProjectHotReloadSessionCallback
    {
        bool SupportsRestart { get; }

        Task OnAfterChangesAppliedAsync(CancellationToken cancellationToken);

        Task<bool> StopProjectAsync(CancellationToken cancellationToken);

        Task<bool> RestartProjectAsync(CancellationToken cancellationToken);

        IDeltaApplier? GetDeltaApplier();
    }
}
