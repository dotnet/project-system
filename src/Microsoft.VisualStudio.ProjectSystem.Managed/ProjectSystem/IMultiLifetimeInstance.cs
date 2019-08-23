// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents an instance that is automatically initialized when its parent <see cref="AbstractMultiLifetimeComponent{T}"/>
    ///     is loaded, or disposed when it is unloaded.
    /// </summary>
    internal interface IMultiLifetimeInstance
    {
        /// <summary>
        ///     Initializes the <see cref="IMultiLifetimeInstance"/>.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        ///     Disposes the <see cref="IMultiLifetimeInstance"/>.
        /// </summary>
        Task DisposeAsync();
    }
}
