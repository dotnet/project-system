// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{

    /// <summary>
    /// Interface definition used by the ProjectDebugger to decide how to launch a profile. The order
    /// of the imports is important in that this determines the order which profiles will be tested
    /// for support 
    /// </summary>
    public interface IDebugProfileLaunchTargetsProvider
    {
        bool SupportsProfile(ILaunchProfile profile);

        Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);

        // Called just prior to launch to allow the provider to do additional work.
        Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);

        // Called right after launch to allow the provider to do additional work.
        Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
    }
}

