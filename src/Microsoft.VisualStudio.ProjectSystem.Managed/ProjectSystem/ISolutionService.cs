// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Operations and properties related to the solution.
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

        /// <summary>
        ///     Gets the VS solution object.
        /// </summary>
        /// <remarks>
        ///     Must be called from the main thread.
        /// </remarks>
        IVsSolution Solution { get; }

        /// <summary>
        ///     Creates a new subscription for solution events that will call back via <paramref name="eventListener" />.
        /// </summary>
        /// <param name="eventListener">The callback for events.</param>
        /// <param name="cancellationToken">A token whose cancellation marks lost interest in the result of this task.</param>
        /// <returns>An object that unsubscribes when disposed.</returns>
        Task<IAsyncDisposable> SubscribeAsync(IVsSolutionEvents eventListener, CancellationToken cancellationToken = default);
    }
}
