// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Implementation of <see cref="IDebugProfileLaunchTargetsProvider"/> may optionally
/// implement this version of this interface to get access to <see cref="IVsLaunchedProcess"/>
/// in order to temrinate the process while ignoring existing debug option flags.
/// </summary>
public interface IDebugProfileLaunchTargetsProvider5 : IDebugProfileLaunchTargetsProvider4
{
    /// <summary>
    /// Called right after launch to allow the provider to do additional work.
    /// This method will only be invoked when there's exactly one <see cref="IDebugLaunchSettings"/>, one <see cref="IVsLaunchedProcess"/> and one <see cref="VsDebugTargetProcessInfo"/>
    /// returned from debugger.
    /// </summary>
    Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IDebugLaunchSettings debugLaunchSetting, IVsLaunchedProcess vsLaunchedProcess, VsDebugTargetProcessInfo processInfo);
}
