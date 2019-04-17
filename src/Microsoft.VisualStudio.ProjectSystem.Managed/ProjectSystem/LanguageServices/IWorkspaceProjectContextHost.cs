// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts an <see cref="IWorkspaceProjectContext"/> for a <see cref="ConfiguredProject"/> and provides consumers access to it.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IWorkspaceProjectContextHost
    {
        /// <summary>
        ///     Returns a task that will complete when current <see cref="IWorkspaceProjectContextHost"/> has completed
        ///     loading.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and the <see cref="ConfiguredProject"/> is unloaded.
        ///     <para>
        ///         -or
        ///     </para>
        ///     The result is awaited and <paramref name="cancellationToken"/> is cancelled.
        /// </exception>
        /// <remarks>
        ///     This method does not initiate loading of the <see cref="IWorkspaceProjectContextHost"/>, however,
        ///     it will join the load when it starts.
        /// </remarks>
        Task PublishAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Opens the <see cref="IWorkspaceProjectContext"/>, passing it to the specified action for writing.
        /// </summary>
        /// <param name="action">
        ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and the <see cref="ConfiguredProject"/> is unloaded.
        /// </exception>
        /// <exception cref="ActiveProjectConfigurationChangedException">
        ///     The <see cref="IWorkspaceProjectContextHost"/> represents the active one, and 
        ///     the configuration changed.
        /// </exception>
        Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action);

        /// <summary>
        ///     Opens the <see cref="IWorkspaceProjectContext"/>, passing it to the specified action for writing.
        /// </summary>
        /// <param name="action">
        ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
        /// </param>
        /// <returns>
        ///     The result of <paramref name="action"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and the <see cref="ConfiguredProject"/> is unloaded.
        /// </exception>
        /// <exception cref="ActiveProjectConfigurationChangedException">
        ///     The <see cref="IWorkspaceProjectContextHost"/> represents the active one, and 
        ///     the configuration changed.
        /// </exception>
        Task<T> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action);
    }
}
