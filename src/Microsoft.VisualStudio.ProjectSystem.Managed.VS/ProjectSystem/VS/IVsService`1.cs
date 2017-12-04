// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a required Visual Studio proffered service.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the service to retrieve and return from <see cref="Value"/>.
    /// </typeparam>
    internal interface IVsService<T>
    {
        /// <summary>
        ///     Gets the required service object associated with <typeparamref name="T"/>.
        /// </summary>
        ///<exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        /// <value>
        ///     The service <see cref="object"/> associated with <typeparamref name="T"/>;
        ///     otherwise, throws an exception if it is not present.
        /// </value>
        /// <exception cref="Exception">
        ///     The service could not be found.
        /// </exception>
        T Value
        {
            get;
        }
    }
}
