// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Interface definition used by StartupProjectRegistrar to display only debuggable projects
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Extension)]
    public interface IStartupProjectProvider
    {
        /// <summary>
        /// Returns true if this is a project is debuggable and should appear in the startup list.
        /// </summary>
        Task<bool> IsProjectDebuggableAsync(DebugLaunchOptions launchOptions);
    }
}
