// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

/// <summary>
/// Implements a web socket server which allows connections from js code injected in the response stream by asp.net core middleware. This
/// enables sending a refresh command to the browser when code changes.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IBrowserRefreshService
{
    /// <summary>
    /// Returns true if the current running app has browserLink injected
    /// </summary>
    bool BrowserLinkInjected { get; }

    /// <summary>
    /// Starts the server if it hasn't been started yet.
    /// It is safe to call this multiple times.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask StartServerAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Configures the launch environment to enable browser refresh.
    /// Must be called after <see cref="StartServerAsync(CancellationToken)"/> is called.
    /// </summary>
    ValueTask ConfigureLaunchEnvironmentAsync(IDictionary<string, string> builder, bool enableHotReload, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message to the browser to that updates were applied. This is preferable to the refresh messages since is knowledgeable about
    /// whether blazor is loaded in the page and will do the right thing. Note that this function throws on failure
    /// </summary>
    ValueTask RefreshBrowserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a ping message to all connected browsers.
    /// It will throw InvalidOperationException if the browser refresh server is not started.
    /// </summary>
    /// <param name="cancellationToken"></param>
    ValueTask SendPingMessageAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a reload message to all connected browsers.
    /// It will throw InvalidOperationException if the browser refresh server is not started.
    /// </summary>
    /// <returns></returns>
    ValueTask SendReloadMessageAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a wait message to all connected browsers.
    /// It will throw InvalidOperationException if the browser refresh server is not started.
    /// </summary>
    ValueTask SendWaitMessageAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously updates the static assets referenced by the specified URLs.
    /// </summary>
    /// <remarks>If the operation is canceled via the provided cancellation token, the returned task will be
    /// in a canceled state. The method does not guarantee the order in which assets are updated.</remarks>
    /// <param name="assetUrls">A collection of URLs identifying the static assets to update. Each URL must be a valid, non-empty string.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the update operation.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    ValueTask UpdateStaticAssetsAsync(IEnumerable<string> assetUrls, CancellationToken cancellationToken);
}
