// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.ProjectSystem.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web;

internal sealed class VisualStudioBrowserRefreshServerAccessor(
    ILogger logger,
    ILoggerFactory loggerFactory,
    string projectName,
    int port,
    int sslPort,
    string virtualDirectory)
    : AbstractBrowserRefreshServerAccessor
{
    internal override AbstractBrowserRefreshServer Server { get; } = new VisualStudioBrowserRefreshServer(
        logger,
        loggerFactory,
        projectName,
        port,
        sslPort,
        virtualDirectory);
}
