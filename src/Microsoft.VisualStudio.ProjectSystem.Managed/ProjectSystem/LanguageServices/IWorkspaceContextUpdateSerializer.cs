// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    // Added in response to https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1370097

    /// <summary>
    /// Performs language service workspace updates serially across the entire solution.
    /// </summary>
    /// <remarks>
    /// Roslyn internally uses blocking locks to protect access to its solution-level workspace data.
    /// When opening a solution with many projects, each project make requests to update workspaces.
    /// Those updates pass through this interface so that we can serialize them in order to reduce
    /// blocking lock contention on the thread pool from concurrent update requests.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System)]
    internal interface IWorkspaceContextUpdateSerializer
    {
        Task ApplyUpdateAsync(Func<Task> updateFunc);
    }
}
