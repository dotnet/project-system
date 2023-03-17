// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

/// <summary>
/// MEF Export that can be used to be notified when a project enters and exits hot reload 
/// </summary>
[Export(typeof(IProjectHotReloadNotificationService))]
internal class ProjectHotReloadNotificationService : IProjectHotReloadNotificationService
{
    [ImportingConstructor]
    public ProjectHotReloadNotificationService(UnconfiguredProject _)
    {
        // Importing UnconfiguredProject to ensure this part is in UnconfiguredProject scope
    }

    public event AsyncEventHandler<bool>? HotReloadStateChangedAsync;

    public bool IsProjectInHotReload { get; private set; }

    public async Task SetHotReloadStateAsync(bool isInHotReload)
    {
        IsProjectInHotReload = isInHotReload;

        var localEvent = HotReloadStateChangedAsync;
        if (localEvent is not null)
        {
            try
            {
                await localEvent.InvokeAsync(this, isInHotReload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail($"HotReloadStartChanged handler threw an exception {ex}");
            }
        }
    }
}

