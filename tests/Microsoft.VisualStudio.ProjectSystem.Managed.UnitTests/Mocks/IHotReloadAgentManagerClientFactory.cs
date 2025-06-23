﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS;
internal class IHotReloadAgentManagerClientFactory
{
    public static IHotReloadAgentManagerClient Create()
    {
        var mock = new Mock<IHotReloadAgentManagerClient>();
        return mock.Object;
    }
}
