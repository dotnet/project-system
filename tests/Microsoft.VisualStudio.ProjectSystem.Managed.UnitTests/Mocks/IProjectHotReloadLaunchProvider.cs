// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal static class IProjectHotReloadLaunchProviderFactory
{
    public static IProjectHotReloadLaunchProvider Create()
    {
        var mock = new Mock<IProjectHotReloadLaunchProvider>();
        return mock.Object;
    }
}
