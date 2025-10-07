// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

internal static class IProjectHotReloadSessionWebAssemblyCallbackFactory
{
    public static IProjectHotReloadSessionWebAssemblyCallback Create()
    {
        var mock = new Mock<IProjectHotReloadSessionWebAssemblyCallback>();

        var middlewarePath = Path.GetTempPath();
        var logger = new Mock<ILogger>().Object;
        var loggerFactory = new Mock<ILoggerFactory>().Object;

        var server = new Mock<AbstractBrowserRefreshServer>(middlewarePath, logger, loggerFactory).Object;

        mock.Setup(session => session.BrowserRefreshServerAccessor)
            .Returns(new TestBrowserRefreshServerAccessor(server));

        return mock.Object;
    }
}
