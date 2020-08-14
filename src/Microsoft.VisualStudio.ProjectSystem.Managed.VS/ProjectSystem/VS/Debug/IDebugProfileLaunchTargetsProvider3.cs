// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
    public interface IDebugProfileLaunchTargetsProvider3
    {
        Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
    }
}
