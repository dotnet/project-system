// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     A project-service level component that provides methods for accessing the MSBuild evaluation and 
    ///     construction models for a <see cref="UnconfiguredProject"/> or <see cref="ConfiguredProject"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectAccessor
    {
        /// <summary>
        ///     Opens the MSBuild project construction model for the specified project, passing it to the specified action for reading.
        /// </summary>
        /// <param name="project">
        ///     The <see cref="UnconfiguredProject"/> whose underlying MSBuild object model is required.
        /// </param>
        /// <param name="action">
        ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
        /// </param>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <returns>
        ///     The result of executing <paramref name="action"/> over the <see cref="ProjectRootElement"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="project"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///     NOTE: To avoid deadlocks, do not call arbitrary services or asynchronous code within <paramref name="action"/>.
        /// </remarks>
        Task<TResult> OpenProjectXmlForReadAsync<TResult>(UnconfiguredProject project, Func<ProjectRootElement, TResult> action, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Opens the MSBuild project construction model for the specified project, passing it to the specified action for writing.
        /// </summary>
        /// <param name="project">
        ///     The <see cref="UnconfiguredProject"/> whose underlying MSBuild object model is required.
        /// </param>
        /// <param name="action">
        ///     The <see cref="Action{T}"/> to run while holding the lock.
        /// </param>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <returns>
        ///     The result of executing <paramref name="action"/> over the <see cref="ProjectRootElement"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="project"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///     NOTE: To avoid deadlocks, do not call arbitrary services or asynchronous code within <paramref name="action"/>.
        /// </remarks>
        Task OpenProjectXmlForWriteAsync(UnconfiguredProject project, Action<ProjectRootElement> action, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Opens the MSBuild project evaluation model for the specified project, passing it to the specified action for reading.
        /// </summary>
        /// <param name="project">
        ///     The <see cref="ConfiguredProject"/> whose underlying MSBuild object model is required.
        /// </param>
        /// <param name="action">
        ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
        /// </param>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <returns>
        ///     The result of executing <paramref name="action"/> over the <see cref="Project"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="project"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///     NOTE: To avoid deadlocks, do not call arbitrary services or asynchronous code within <paramref name="action"/>.
        /// </remarks>
        Task<TResult> OpenProjectForReadAsync<TResult>(ConfiguredProject project, Func<Project, TResult> action, CancellationToken cancellationToken = default(CancellationToken));
    }
}
