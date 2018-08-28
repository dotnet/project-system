// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffered service that must be used on the UI thread.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the service to retrieve and return from <see cref="Value"/>.
    /// </typeparam>
    internal interface IVsUIService<T>
    {
        /// <summary>
        ///     Gets the service object associated with <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        /// <value>
        ///     The service <see cref="object"/> associated with <typeparamref name="T"/>;
        ///     otherwise, <see langword="null"/> if it is not present.
        /// </value>
        T Value
        {
            get;
        }
    }
}
