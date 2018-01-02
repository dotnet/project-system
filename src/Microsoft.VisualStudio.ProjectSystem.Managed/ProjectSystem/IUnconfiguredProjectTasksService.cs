// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides methods for that assist in managing project-related background tasks. 
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal interface IUnconfiguredProjectTasksService
    {   // Provides unit testable versions of CommonProjectSystemTools.LoadedProjectAsync

        /// <summary>
        ///     Provides protection for an operation that the project will not close before the completion of some task.
        /// </summary>
        /// <param name="action">
        ///     The action to execute within the context of a loaded project.
        /// </param>
        /// <exception cref="OperationCanceledException">
        ///     Thrown if the project was already unloaded before this method was invoked.
        /// </exception>
        Task LoadedProjectAsync(Func<Task> action);

        /// <summary>
        ///     Provides protection for an operation that the project will not close before the completion of some task.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of value returned by the joinable.
        /// </typeparam>
        /// <param name="action">
        ///     The action to execute within the context of a loaded project.
        /// </param>
        /// <exception cref="OperationCanceledException">
        ///     Thrown if the project was already unloaded before this method was invoked.
        /// </exception>
        Task<T> LoadedProjectAsync<T>(Func<Task<T>> action);
    }
}
