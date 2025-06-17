// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Implementations of <see cref="IDebugProfileLaunchTargetsProvider"/> may optionally
/// implement this version of the interface to expose additional capabilities.
/// </summary>
public interface IDebugProfileLaunchTargetsProvider2
{
    /// <summary>
    /// Called in response to an F5/Ctrl+F5 operation to get the debug launch settings to pass to the
    /// debugger for the active profile.
    /// </summary>
    /// <remarks>
    /// Implementing this optional method (in comparison to <see cref="IDebugProfileLaunchTargetsProvider.QueryDebugTargetsAsync"/>
    /// which must also be implemented) allows the provider to distinguish calls that are happening as part of a debug launch.
    ///
    /// Specifically:
    /// <list type="bullet">
    ///   <item><see cref="IDebugProfileLaunchTargetsProvider.QueryDebugTargetsAsync"/> is called via <c>IVsDebuggableProjectCfg:QueryDebugTargets</c>.</item>
    ///   <item><see cref="QueryDebugTargetsForDebugLaunchAsync"/> is called via <c>IVsDebuggableProjectCfg:DebugLaunch</c>.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsForDebugLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
}
