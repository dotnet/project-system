// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = Composition.ImportCardinality.ExactlyOne)]
    public interface IProjectHotReloadAgent
    {
        IProjectHotReloadSession? CreateHotReloadSession(string id, int variant, string runtimeVersion, IProjectHotReloadSessionCallback callback);

        IProjectHotReloadSession? CreateHotReloadSession(string id, string runtimeVersion, IProjectHotReloadSessionCallback callback);
    }
}
