// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents a service that listen for project loaded events in a host.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private)]
    internal interface ILoadedInHostListener
    {
        /// <summary>
        ///     Starts listening for project events in a host.
        /// </summary>
        /// <returns>
        ///     Once this method has been called once, all future calls are no-ops.
        /// </returns>
        Task StartListeningAsync();
    }
}
