// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Net;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal sealed class VisualStudioBrowserRefreshServer(
    ILogger logger,
    ILoggerFactory loggerFactory,
    string projectName,
    int port,
    int sslPort,
    string virtualDirectory)
    : AbstractBrowserRefreshServer(GetMiddlewareAssemblyPath(), logger, loggerFactory)
{
    private const string MiddlewareTargetFramework = "net6.0";

    private static string GetMiddlewareAssemblyPath()
        => ProjectHotReloadSession.GetInjectedAssemblyPath(MiddlewareTargetFramework, "Microsoft.AspNetCore.Watch.BrowserRefresh");

    protected override bool SuppressTimeouts
        => false;

    // for testing
    internal Task? WebSocketListeningTask { get; private set; }

    protected override ValueTask<WebServerHost> CreateAndStartHostAsync(CancellationToken cancellationToken)
    {
        var httpListener = CreateListener(projectName, port, sslPort);
        WebSocketListeningTask = ListenAsync(cancellationToken);

        return new(new WebServerHost(httpListener, GetWebSocketUrls(projectName, port, sslPort), virtualDirectory));

        async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                httpListener.Start();

                while (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogDebug("Waiting for a browser connection");

                    // wait for incoming request:
                    var context = await httpListener.GetContextAsync();
                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        continue;
                    }

                    try
                    {
                        // Accepting Socket Next request. If the context has a "Sec-WebSocket-Protocol" header it passes back in the AcceptWebSocket
                        var protocol = context.Request.Headers["Sec-WebSocket-Protocol"];
                        var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: protocol).WithCancellation(cancellationToken);

                        _ = OnBrowserConnected(webSocketContext.WebSocket, webSocketContext.SecWebSocketProtocols.FirstOrDefault());
                    }
                    catch (Exception e) when (e is not (OperationCanceledException or ObjectDisposedException))
                    {
                        Logger.LogError("Accepting web socket exception: {Message}", e.Message);

                        context.Response.StatusCode = 500;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException)
            {
                // expected during shutdown
            }
            catch (Exception e)
            {
                Logger.LogError("Unexpected HttpListener exception: {Message}", e.Message);
            }
        }
    }

    private static HttpListener CreateListener(string projectName, int port, int sslPort)
    {
        var httpListener = new HttpListener();

        httpListener.Prefixes.Add($"http://localhost:{port}/{projectName}/");
        if (sslPort >= 0)
        {
            httpListener.Prefixes.Add($"https://localhost:{sslPort}/{projectName}/");
        }

        return httpListener;
    }

    private static ImmutableArray<string> GetWebSocketUrls(string projectName, int port, int sslPort)
    {
        return sslPort >= 0 ? [GetWebSocketUrl(port, isSecure: false), GetWebSocketUrl(sslPort, isSecure: true)] : [GetWebSocketUrl(port, isSecure: false)];

        string GetWebSocketUrl(int port, bool isSecure)
            => $"{(isSecure ? "wss" : "ws")}://localhost:{port}/{projectName}/";
    }
}
