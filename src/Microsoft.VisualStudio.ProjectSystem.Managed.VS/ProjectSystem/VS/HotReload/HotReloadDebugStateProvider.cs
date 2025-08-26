// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

// TODO: Replace with IDebuggerStateService
// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2571211

[Export(typeof(IHotReloadDebugStateProvider))]
[method: ImportingConstructor]
internal sealed class HotReloadDebugStateProvider(
    IProjectThreadingService threadingService,
    IVsUIService<SVsShellDebugger, IVsDebugger> debugger) : IHotReloadDebugStateProvider
{
    public async ValueTask<bool> IsSuspendedAsync(CancellationToken cancellationToken)
    {
        await threadingService.SwitchToUIThread(cancellationToken);

        var dbgmode = new DBGMODE[1];
        return ErrorHandler.Succeeded(debugger.Value.GetMode(dbgmode)) &&
               (dbgmode[0] & ~DBGMODE.DBGMODE_EncMask) == DBGMODE.DBGMODE_Break;
    }
}
