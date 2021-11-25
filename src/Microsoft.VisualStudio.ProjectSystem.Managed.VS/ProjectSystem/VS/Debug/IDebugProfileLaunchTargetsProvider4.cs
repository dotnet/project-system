// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Optional interface that allows the provider to receive more information about
    /// launch settings and started processes.
    /// </summary>
    public interface IDebugProfileLaunchTargetsProvider4
    {
        /// <summary>
        /// Called just prior to launch to allow the provider to do additional work.
        /// </summary>
        /// <remarks>
        /// Note this will be called instead of <see cref="IDebugProfileLaunchTargetsProvider.OnBeforeLaunchAsync(DebugLaunchOptions, ILaunchProfile)"/>.
        /// </remarks>
        Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IReadOnlyList<IDebugLaunchSettings> debugLaunchSettings);

        /// <summary>
        /// Called right after launch to allow the provider to do additional work.
        /// </summary>
        /// <remarks>
        /// Note this will be called instead of <see cref="IDebugProfileLaunchTargetsProvider.OnAfterLaunchAsync(DebugLaunchOptions, ILaunchProfile)"/>.
        /// </remarks>
        Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IReadOnlyList<VsDebugTargetProcessInfo> processInfos);
    }
}
