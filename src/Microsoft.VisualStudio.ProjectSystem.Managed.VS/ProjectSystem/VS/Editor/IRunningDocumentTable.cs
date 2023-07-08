// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// Allows subscribing to running document table (RDT) events via the <see cref="IVsRunningDocumentTable"/>
    /// family of event handler interfaces.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.System)]
    internal interface IRunningDocumentTable
    {
        /// <summary>
        /// Creates a new subscription for RDT events that will call back via <paramref name="eventListener" />.
        /// </summary>
        /// <param name="eventListener">The callback for events. Note that it may also implement additional version(s) of this interface.</param>
        /// <returns>An object that unsubscribes when disposed.</returns>
        Task<IAsyncDisposable> SubscribeEventsAsync(IVsRunningDocTableEvents eventListener);
    }
}
