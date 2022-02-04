// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Allows subscribing to solution build manager events via the <see cref="IVsUpdateSolutionEvents"/>
    /// family of event handler interfaces.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.System)]
    internal interface ISolutionBuildEvents
    {
        /// <summary>
        /// Creates a new subscription for build events that will call back via <paramref name="eventListener" />.
        /// </summary>
        /// <param name="eventListener">The callback for events. Note that it may also implement additional version(s) of this interface.</param>
        /// <returns>An object that unsubscribes when disposed.</returns>
        Task<IAsyncDisposable> SubscribeAsync(IVsUpdateSolutionEvents eventListener);
    }
}
