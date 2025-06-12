// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Implementations of this interface provide supports for a particular launch profile command (such as "executable", "project", ...).
/// </summary>
/// <remarks>
/// When multiple exports of this interface are available, they are tested in precedence order.
/// 
/// This interface is used by <see cref="LaunchProfilesDebugLaunchProvider"/> for operations on launch profiles that involve logic specific to the type of profile.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
public interface IDebugProfileLaunchTargetsProvider
{
    /// <summary>
    /// Gets whether this provider supports <paramref name="profile"/>.
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

