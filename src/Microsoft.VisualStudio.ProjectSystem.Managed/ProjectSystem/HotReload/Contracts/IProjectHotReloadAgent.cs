// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadAgent
{
    /// <param name="runtimeVersion">Format: "Major.Minor"</param>
    IProjectHotReloadSession? CreateHotReloadSession(string id, int variant, string runtimeVersion, IProjectHotReloadSessionCallback callback);

    /// <param name="runtimeVersion">Format: "Major.Minor"</param>
    IProjectHotReloadSession? CreateHotReloadSession(string id, string runtimeVersion, IProjectHotReloadSessionCallback callback);
}

[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface IProjectHotReloadAgent2 : IProjectHotReloadAgent
{
    IProjectHotReloadSession CreateHotReloadSession(
        string id,
        int variant,
        string runtimeVersion,
        ConfiguredProject configuredProject,
        IProjectHotReloadLaunchProvider launchProvider,
        IProjectHotReloadBuildManager buildManager,
        IProjectHotReloadSessionCallback callback,
        ILaunchProfile launchProfile,
        DebugLaunchOptions debugLaunchOptions);
}

