// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a single property; <see cref="LoadedInHost"/>, which completes when the host
    ///     recognizes that the solution is loaded.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private)]
    internal interface ISolutionService
    {
        /// <summary>
        ///     Gets a task that completes when the host recognizes that the solution is loaded.
        /// </summary>
        /// <remarks>
        ///     Use <see cref="IUnconfiguredProjectTasksService.SolutionLoadedInHost"/> if
        ///     within project context.
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when host is closed without a solution being loaded.
        /// </exception>
        Task LoadedInHost
        {
            get;
        }
    }
}
