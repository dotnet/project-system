// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        /// <remarks>
        /// Note that if <see cref="IDebugProfileLaunchTargetsProvider4"/> is also implemented, <see cref="IDebugProfileLaunchTargetsProvider4.OnBeforeLaunchAsync(DebugLaunchOptions, ILaunchProfile, IReadOnlyList{IDebugLaunchSettings})"/>
        /// will be called instead of this.
        /// </remarks>
        Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);

        /// <summary>
        /// Called right after launch to allow the provider to do additional work.
        /// </summary>
        /// <remarks>
        /// Note that if <see cref="IDebugProfileLaunchTargetsProvider4"/> is also implemented, <see cref="IDebugProfileLaunchTargetsProvider4.OnAfterLaunchAsync(DebugLaunchOptions, ILaunchProfile, IReadOnlyList{Shell.Interop.VsDebugTargetProcessInfo})"/>
        /// will be called instead of this.
        /// </remarks>
        Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile);
    }
}

