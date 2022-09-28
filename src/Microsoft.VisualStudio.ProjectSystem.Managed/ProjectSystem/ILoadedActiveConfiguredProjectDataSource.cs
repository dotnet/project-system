// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
/// This block publish data out loaded configuration when active configured project is changed. 
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = Composition.ImportCardinality.ExactlyOne)]
internal interface ILoadedActiveConfiguredProjectDataSource : IProjectValueDataSource<IEnumerable<ConfiguredProject>>
{
}
