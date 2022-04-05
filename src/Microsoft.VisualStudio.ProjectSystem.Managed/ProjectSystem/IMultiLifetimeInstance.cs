// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
