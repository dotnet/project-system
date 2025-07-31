// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

internal static class IProjectHotReloadAgentFactory
{
    public static IProjectHotReloadAgent2 Create(IProjectHotReloadSession? session = null)
    {
        var mock = new Mock<IProjectHotReloadAgent2>();

        if (session is null)
        {
            session = IProjectHotReloadSessionFactory.Create();
        }

        mock.Setup(agent => agent.CreateHotReloadSession(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<ConfiguredProject>(),
            It.IsAny<IProjectHotReloadLaunchProvider>(),
            It.IsAny<IProjectHotReloadBuildManager>(),
            It.IsAny<IProjectHotReloadSessionCallback>(),
            It.IsAny<ILaunchProfile>(),
            It.IsAny<DebugLaunchOptions>()))
            .Returns(session);

        mock.Setup(agent => agent.CreateHotReloadSession(
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<string>(),
            It.IsAny<ConfiguredProject>(),
            It.IsAny<IProjectHotReloadLaunchProvider>(),
            It.IsAny<IProjectHotReloadBuildManager>(),
            It.IsAny<IProjectHotReloadSessionCallback>(), 
            It.IsAny<ILaunchProfile>(), 
            It.IsAny<DebugLaunchOptions>()))
            .Returns(session);

        return mock.Object;
    }
}
