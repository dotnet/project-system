// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
///     Hosts the "active" <see cref="IWorkspaceProjectContext"/> for an <see cref="UnconfiguredProject"/>
///     and provides consumers access to modify it.
/// </summary>
/// <remarks>
///     <para>
///         The "active" <see cref="IWorkspaceProjectContext"/> for an <see cref="UnconfiguredProject"/> is the one associated
///         with the solution's active configuration.
///     </para>
///     <para>
///         NOTE: This is distinct from the "active" context for the editor which is tracked via <see cref="IActiveEditorContextTracker"/>.
///     </para>
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IWorkspaceWriter
{
    /// <summary>
    ///    Gets a value indicating whether the language service is enabled for this project.
    /// </summary>
    /// <remarks>
    ///    Users of this interface should use this method to validate that the language service
    ///    is enabled for this project before attempting to use the other members of this interface.
    ///    Attempting to use other members when the language service is disabled will result in
    ///    an exception being thrown.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     Registers a loss of interest in the operation.
    /// </param>
    /// <returns>
    ///     A task who's completed result indicates whether the language service is enabled or not
    ///     for this project.
    /// </returns>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Completes when a write lock can be requested for the active workspace.
    /// </summary>
    /// <remarks>
    ///     This method does not initiate loading of the workspace's context, however,
    ///     it will join the load when it starts.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     Registers a loss of interest in the operation.
    /// </param>
    /// <exception cref="OperationCanceledException">
    ///     The result is awaited, and either the project is unloaded or <paramref name="cancellationToken"/> is cancelled.
    /// </exception>
    Task WhenInitialized(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Acquires a write lock and invoked <paramref name="action"/> while held.
    ///     The action may modify the workspace before returning.
    /// </summary>
    /// <param name="action">
    ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
    /// </param>
    /// <param name="cancellationToken">
    ///     Registers a loss of interest in the operation.
    /// </param>
    /// <exception cref="OperationCanceledException">
    ///     The result is awaited and the project is unloaded.
    /// </exception>
    Task WriteAsync(Func<IWorkspace, Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Acquires a write lock and invoked <paramref name="func"/> while held.
    ///     The action may modify the workspace before returning.
    /// </summary>
    /// <param name="func">
    ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
    /// </param>
    /// <returns>
    ///     The result of <paramref name="func"/>.
    /// </returns>
    /// <param name="cancellationToken">
    ///     Registers a loss of interest in the operation.
    /// </param>
    /// <exception cref="OperationCanceledException">
    ///     The result is awaited and the project is unloaded.
    /// </exception>
    Task<T> WriteAsync<T>(Func<IWorkspace, Task<T>> func, CancellationToken cancellationToken = default);
}
