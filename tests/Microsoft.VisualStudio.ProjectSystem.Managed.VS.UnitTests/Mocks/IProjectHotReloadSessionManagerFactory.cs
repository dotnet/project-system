// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

internal static class IProjectHotReloadSessionManagerFactory
{
    public static IProjectHotReloadSessionManager Create(bool canCreatePendingSession = true)
    {
        var mock = new Mock<IProjectHotReloadSessionManager>();

        mock.Setup(static manager => manager.TryCreatePendingSessionAsync(
            It.IsAny<IProjectHotReloadLaunchProvider>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<DebugLaunchOptions>(),
            It.IsAny<ILaunchProfile>()))
            .ReturnsAsync(canCreatePendingSession);

        mock.Setup(manager => manager.ActivateSessionAsync(It.IsAny<IVsLaunchedProcess>(), It.IsAny<VsDebugTargetProcessInfo>()))
            .Returns(Task.CompletedTask);

        return mock.Object;
    }
}
