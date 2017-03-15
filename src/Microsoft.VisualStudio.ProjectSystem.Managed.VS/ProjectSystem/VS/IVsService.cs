// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffored service.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the service to retrieve and return from <see cref="IVsService{TInterfaceType, TServiceType}.Value"/>.
    /// </typeparam>
    internal interface IVsService<T> : IVsService<T, T>
    {
    }

    /// <summary>
    ///     Provides access to a Visual Studio proffored service.
    /// </summary>
    /// <typeparam name="TInterfaceType">
    ///     The type of the service to return from <see cref="Value"/>
    /// </typeparam>
    /// <typeparam name="TServiceType">
    ///     The type of the service to retrieve.
    /// </typeparam>
    internal interface IVsService<TInterfaceType, TServiceType>
    {
        /// <summary>
        ///     Gets the service object of type <typeparamref name="TInterfaceType"/>, associated with <typeparamref name="TServiceType"/>.
        /// </summary>
        ///<exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        TInterfaceType Value
        {
            get;
        }
    }
}
