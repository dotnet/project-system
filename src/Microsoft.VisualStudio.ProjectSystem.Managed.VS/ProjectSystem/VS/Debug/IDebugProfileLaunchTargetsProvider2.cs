// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Optional interface that can be cast from IDebugProfileLaunchTargetsProvider for those implementations which need to distinguish
    /// calls to QueryDebugTargetsAsync that originate from IVsDebuggableProjectCfg:QueryDebugTargets, from calls that originate from 
    /// IVsDebuggableProjectCfg:DebugLaunch. If this interface is implemented, calls that originate from a debugLaunch will call 
    /// QueryDebugTargetsForDebugLaunchAsync(). Calls from QueryDebugTargets will call IDebugProfileLaunchTargetsProvider:QueryDebugTargetsAsync
    /// </summary>
    public interface IDebugProfileLaunchTargetsProvider2
    {
        /// <summary>
        /// Called in response to an F5/Ctrl+F5 operation to get the debug launch settings to pass to the
        /// debugger for the active profile.
        /// </summary>
        Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsForDebugLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
    }
}
