// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

public interface IBrowserRefreshServerAccessor : IDisposable
{
    ValueTask StartServerAsync(CancellationToken cancellationToken);

    void ConfigureLaunchEnvironment(IDictionary<string, string> builder, bool enableHotReload);

    ValueTask RefreshBrowserAsync(CancellationToken cancellationToken);

    ValueTask SendPingMessageAsync(CancellationToken cancellationToken);

    ValueTask SendReloadMessageAsync(CancellationToken cancellationToken);

    ValueTask SendWaitMessageAsync(CancellationToken cancellationToken);

    ValueTask UpdateStaticAssetsAsync(IEnumerable<string> relativeUrls, CancellationToken cancellationToken);
}
