// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[InternalImplementationOnly]
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadAgent
{
    /// <summary>
    /// Creates Hot Reload session.
    /// </summary>
    /// <param name="name">Session name used for logging.</param>
    /// <param name="id">Unique session id used for logging.</param>
    /// <param name="configuredProject">Associated project.</param>
    /// <param name="callback">Used to attach custom data and behavior to the session.</param>
    /// <param name="launchProfile">Launch profile used to launch the process.</param>
    /// <param name="debugLaunchOptions">Debugger options used to launch the process.</param>
    /// <returns>New session instance.</returns>
    IProjectHotReloadSession CreateHotReloadSession(
        string name,
        int id,
        ConfiguredProject configuredProject,
        IProjectHotReloadSessionCallback callback,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions);
}

public static class IProjectHotReloadAgentExtensions
{
    // Once all callers switch to this extension method we can clean up the interfaces
    public static IProjectHotReloadSession CreateHotReloadSession(
        this IProjectHotReloadAgent agent,
        string id,
        int variant,
        ConfiguredProject configuredProject,
        IProjectHotReloadSessionCallback callback,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions)
    {
        return agent.CreateHotReloadSession(id, variant, configuredProject, callback, launchProfile, debugLaunchOptions);
    }
}
