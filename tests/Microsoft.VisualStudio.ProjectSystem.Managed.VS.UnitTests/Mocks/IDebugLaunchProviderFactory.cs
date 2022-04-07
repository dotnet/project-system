// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public static class IDebugLaunchProviderFactory
    {
        public static IDebugLaunchProvider ImplementIsProjectDebuggableAsync(Func<bool> action)
        {
            var mock = new Mock<IDebugLaunchProvider>();

            mock.As<IStartupProjectProvider>()
                .Setup(d => d.CanBeStartupProjectAsync(It.IsAny<DebugLaunchOptions>()))
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
