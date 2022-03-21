// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Optional interface that is used to tell whether a project should appear in
    /// the Startup drop down list.
    /// </summary>
    public interface IDebugProfileLaunchTargetsProvider3
    {
        Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
    }
}
