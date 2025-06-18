// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal interface IProjectHotReloadLaunchProvider
{
    Task LaunchWithProfileAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
}
