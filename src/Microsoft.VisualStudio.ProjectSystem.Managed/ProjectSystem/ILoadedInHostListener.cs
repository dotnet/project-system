// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents a service that listen for project loaded events in a host.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private)]
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
