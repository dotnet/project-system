// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
///     Provides methods for that assist in managing project-related background tasks. This interface replaces
///     the IProjectAsyncLoadDashboard interface from CPS.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
internal interface IUnconfiguredProjectTasksService
{
    /// <summary>
    ///     Gets a token that is cancelled when the project has started to unload.
    /// </summary>
    /// <remarks>
    ///     NOTE: This token is cancelled before <see cref="LoadedProjectAsync"/> actions
    ///     have been completed, so callers can bail early if needed.
    /// </remarks>
    CancellationToken UnloadCancellationToken { get; }

    /// <summary>
    ///     Gets a task that completes when the host recognizes that the solution is loaded,
    ///     or is cancelled if the project is unloaded before that occurs.
    /// </summary>
    /// <exception cref="OperationCanceledException">
    ///     Thrown if the project was unloaded before the solution finished loading.
    /// </exception>
    Task SolutionLoadedInHost { get; }

    /// <summary>
    ///     Gets a task that completes when the host recognizes that the project is loaded,
    ///     or is cancelled if the project is unloaded before that occurs.
    /// </summary>
    /// <remarks>
    ///     This task completes after <see cref="PrioritizedProjectLoadedInHost"/> and is intended for
    ///     non-critical services that need to complete after the project has been loaded, but
    ///     after critical services.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    ///     Thrown if the project was unloaded.
    /// </exception>
    Task ProjectLoadedInHost { get; }

    /// <summary>
    ///     Gets a task that completes when the host recognizes that the project is loaded,
    ///     or is cancelled if the project is unloaded before that occurs.
    /// </summary>
    /// <remarks>
    ///     This task completes before <see cref="ProjectLoadedInHost"/> and is intended for
    ///     critical services that need to do work before non-critical services.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    ///     Thrown if the project was unloaded.
    /// </exception>
    Task PrioritizedProjectLoadedInHost { get; }

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

    /// <summary>
    ///     Provides protection for an operation that the project will not be considered loaded in the host before
    ///     the completion of some task.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of value returned by the joinable.
    /// </typeparam>
    /// <param name="action">
    ///     The action to execute before the project is considered loaded in the host.
    /// </param>
    /// <exception cref="OperationCanceledException">
    ///     Thrown if the project was already unloaded before this method was invoked.
    /// </exception>
    Task<T> PrioritizedProjectLoadedInHostAsync<T>(Func<Task<T>> action);

    /// <summary>
    ///     Provides protection for an operation that the project will not be considered loaded in the host before
    ///     the completion of some task.
    /// </summary>
    /// <param name="action">
    ///     The action to execute before the project is considered loaded in the host.
    /// </param>
    /// <exception cref="OperationCanceledException">
    ///     Thrown if the project was already unloaded before this method was invoked.
    /// </exception>
    Task PrioritizedProjectLoadedInHostAsync(Func<Task> action);
}
