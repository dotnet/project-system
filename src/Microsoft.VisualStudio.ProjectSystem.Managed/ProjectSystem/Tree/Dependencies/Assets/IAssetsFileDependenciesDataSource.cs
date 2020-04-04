// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal interface IAssetsFileDependenciesDataSource : IProjectValueDataSource<AssetsFileDependenciesSnapshot>
    {
    }
}
