// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Interface definition used by the ProjectDebugger to decide how to launch a profile. The order
    /// of the imports is important in that this determines the order which profiles will be tested
    /// for support 
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface IDebugProfileLaunchTargetsProvider
    {
        /// <summary>
        /// Return true if this provider supports this profile type.
        /// </summary>
        bool SupportsProfile(ILaunchProfile profile);

        /// <summary>
        /// Called in response to an F5/Ctrl+F5 operation to get the debug launch settings to pass to the
        /// debugger for the active profile
        /// </summary>
        Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);

        /// <summary>
        /// Called just prior to launch to allow the provider to do additional work.
        /// </summary>
        Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);

        /// <summary>
        /// Called right after launch to allow the provider to do additional work.
        /// </summary>
        Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
    }
}

