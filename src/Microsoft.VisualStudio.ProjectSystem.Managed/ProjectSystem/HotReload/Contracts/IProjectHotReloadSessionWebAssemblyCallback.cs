// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

public interface IProjectHotReloadSessionWebAssemblyCallback : IProjectHotReloadSessionCallback
{
    AbstractBrowserRefreshServerAccessor BrowserRefreshServerProvider { get; }

    /// <summary>
    /// Suppresses application of deltas as a workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2570151
    /// </summary>
    bool SuppressDeltaApplication { get; }
}
