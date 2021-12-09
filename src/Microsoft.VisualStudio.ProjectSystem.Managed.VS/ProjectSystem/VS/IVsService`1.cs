// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffered service.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the service to retrieve and return from <see cref="GetValueAsync"/>.
    /// </typeparam>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IVsService<T>
        where T : class?
    {
        /// <summary>
        ///     Gets the service object associated with <typeparamref name="T"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A token whose cancellation indicates that the caller no longer is interested
        ///     in the result. The default is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <value>
        ///     The service <see cref="object"/> associated with <typeparamref name="T"/>;
        ///     otherwise, <see langword="null"/> if it is not present;
        /// </value>
        /// <remarks>
        ///     Note that cancelling <paramref name="cancellationToken"/> will not cancel the
        ///     creation of the service, but will result in an expedient cancellation of the
        ///     returned <see cref="Task"/>, and a dis-joining of any <see cref="JoinableTask"/>
        ///     that may have occurred as a result of this call.
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and <paramref name="cancellationToken"/> is cancelled.
        /// </exception>
        Task<T> GetValueAsync(CancellationToken cancellationToken = default);
    }
}
