// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal interface IHotReloadDebugStateProvider
{
    /// <summary>
    /// True if processes are suspended at a break point.
    /// </summary>
    ValueTask<bool> IsSuspendedAsync(CancellationToken cancellationToken);
}

// TODO:
// IDebuggerStateService is a brokered service but the interface is currently internal.
// We should make it public and implement it in VS Code, then use it here.

#if TODO
[Export(typeof(IHotReloadDebugStateProvider))]
[method: ImportingConstructor]
internal sealed class HotReloadDebugStateProvider(IServiceBroker serviceBroker) : IHotReloadDebugStateProvider
{
    public async ValueTask<bool> IsSuspendedAsync(CancellationToken cancellationToken)
    {
        var debugStateService = await serviceBroker.GetProxyAsync<IDebuggerStateService>(VsDebuggerStateServiceDescriptor);
        if (debugStateService != null)
        {
            var mode = await debugStateService.GetShellModeAsync(CancellationToken.None);

            if (debugStateService is IDisposable dispSvc)
            {
                dispSvc.Dispose();
            }

            return mode == IdeShellMode.Break;
        }


        // Assume not in break mode if no service
        return false;
    }

    /// <summary>
    /// Gets the <see cref="ServiceRpcDescriptor"/> for the BrowserLaunch service.
    /// Use the <see cref="IVsDebuggerStateService"/> interface for the client proxy for this service.
    /// </summary>
    public static ServiceRpcDescriptor VsDebuggerStateServiceDescriptor { get; } = new ServiceJsonRpcDescriptor(
        new ServiceMoniker(VsDebuggerStateService.Moniker, Version.Parse(VsDebuggerStateService.Version)),
        ServiceJsonRpcDescriptor.Formatters.MessagePack,
        ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader);
}
#endif
