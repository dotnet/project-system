// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Implementations of <see cref="IDebugProfileLaunchTargetsProvider"/> may optionally
/// implement this version of the interface to expose additional capabilities.
/// </summary>
public interface IDebugProfileLaunchTargetsProvider3
{
    /// <summary>
    /// Gets whether this launch profile can be a startup project. Startup projects are shown to the user for selection.
    /// </summary>
    Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
}
