// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectHotReloadAgentFactory
    {
        public static IProjectHotReloadAgent Create()
        {
            var mock = new Mock<IProjectHotReloadAgent>();

            mock.Setup(agent => agent.CreateHotReloadSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectHotReloadSessionCallback>()))
                .Returns((IProjectHotReloadSession)null!);

            return mock.Object;
        }
    }
}
