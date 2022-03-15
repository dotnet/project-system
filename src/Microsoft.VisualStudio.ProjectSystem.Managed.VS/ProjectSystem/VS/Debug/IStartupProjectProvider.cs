// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Interface definition used by StartupProjectRegistrar to display only debuggable projects
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Extension)]
    internal interface IStartupProjectProvider
    {
        /// <summary>
        /// Returns true if this project can be selected as a startup project.
        /// </summary>
        Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions);
    }
}
