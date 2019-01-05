// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffered service that must be used on the UI thread.
    /// </summary>
    /// <typeparam name="TService">
    ///     The type of the service to retrieve.
    /// </typeparam>
    /// <typeparam name="TInterface">
    ///     The type of the service to return from <see cref="IVsService{T}.GetValueAsync"/>
    /// </typeparam>
    internal interface IVsUIService<TService, TInterface> : IVsUIService<TInterface>
    {
    }
}
