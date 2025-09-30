// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.DotNet.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

public abstract class AbstractBrowserRefreshServerAccessor : IDisposable
{
    private protected AbstractBrowserRefreshServerAccessor()
    {
    }

    public void Dispose()
        => Server.Dispose();

    public ValueTask StartServerAsync(CancellationToken cancellationToken)
        => Server.StartAsync(cancellationToken);

    public void ConfigureLaunchEnvironment(IDictionary<string, string> builder, bool enableHotReload)
        => Server.ConfigureLaunchEnvironment(builder, enableHotReload);

    public ValueTask RefreshBrowserAsync(CancellationToken cancellationToken)
        => Server.RefreshBrowserAsync(cancellationToken);

    public ValueTask SendPingMessageAsync(CancellationToken cancellationToken)
        => Server.SendPingMessageAsync(cancellationToken);

    public ValueTask SendReloadMessageAsync(CancellationToken cancellationToken)
        => Server.SendReloadMessageAsync(cancellationToken);

    public ValueTask SendWaitMessageAsync(CancellationToken cancellationToken)
        => Server.SendWaitMessageAsync(cancellationToken);

    public ValueTask UpdateStaticAssetsAsync(IEnumerable<string> relativeUrls, CancellationToken cancellationToken)
        => Server.UpdateStaticAssetsAsync(relativeUrls, cancellationToken);

    internal abstract AbstractBrowserRefreshServer Server { get; }
}
