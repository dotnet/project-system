// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffered service that must be used on the UI thread.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the service to retrieve and return from <see cref="Value"/>.
    /// </typeparam>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IVsUIService<T>
        where T : class?
    {
        /// <summary>
        ///     Gets the service object associated with <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        ///     Must be called from the UI thread.
        /// </remarks>
        /// <exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        /// <value>
        ///     The service <see cref="object"/> associated with <typeparamref name="T"/>;
        ///     otherwise, <see langword="null"/> if it is not present.
        /// </value>
        T Value { get; }
    }
}
