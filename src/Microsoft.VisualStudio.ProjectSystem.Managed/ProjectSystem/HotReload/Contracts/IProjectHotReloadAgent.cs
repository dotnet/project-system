// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadAgent
{
    IProjectHotReloadSession? CreateHotReloadSession(string id, int variant, string runtimeVersion, IProjectHotReloadSessionCallback callback);

    IProjectHotReloadSession? CreateHotReloadSession(string id, string runtimeVersion, IProjectHotReloadSessionCallback callback);
}

[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadAgent2 : IProjectHotReloadAgent
{
    IProjectHotReloadSession CreateHotReloadSession(
        string id,
        int variant,
        string? runtimeVersion, // read from project if null
        ConfiguredProject configuredProject,
        IProjectHotReloadLaunchProvider? launchProvider, // ignored, can be null
        IProjectHotReloadBuildManager? buildManager, // ignored, can be null
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
        return ((IProjectHotReloadAgent2)agent).CreateHotReloadSession(
            id, variant, runtimeVersion: null, configuredProject, launchProvider: null, buildManager: null, callback, launchProfile, debugLaunchOptions);
    }
}
