// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectHotReloadSessionFactory
    {
        public static IProjectHotReloadSession Create()
        {
            var mock = new Mock<IProjectHotReloadSession>();

            mock.Setup(session => session.ApplyLaunchVariablesAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            return mock.Object;
        }
    }
}
