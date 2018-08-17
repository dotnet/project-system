// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffered service.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the service to retrieve and return from <see cref="GetValueAsync"/>.
    /// </typeparam>
    internal interface IVsService<T>
    {
        /// <summary>
        ///     Gets the service object associated with <typeparamref name="T"/>.
        /// </summary>
        /// <value>
        ///     The service <see cref="object"/> associated with <typeparamref name="T"/>;
        ///     otherwise, <see langword="null"/> if it is not present;
        /// </value>
        Task<T> GetValueAsync();
    }
}
