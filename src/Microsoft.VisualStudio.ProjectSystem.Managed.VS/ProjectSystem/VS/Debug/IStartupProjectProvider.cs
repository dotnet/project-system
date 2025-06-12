// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Allows <see cref="StartupProjectRegistrar"/> to know which project configurations are debuggable.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Extension)]
internal interface IStartupProjectProvider
{
    /// <summary>
    /// Gets whether this project can be selected as a startup project (the default project for debugging).
    /// </summary>
    Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions);
}
