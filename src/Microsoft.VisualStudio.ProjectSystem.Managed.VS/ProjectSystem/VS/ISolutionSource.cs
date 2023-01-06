// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

/// <summary>
/// Provides access to the <see cref="IVsSolution"/> object, and allows subscribing to solution
/// events via the <see cref="IVsSolutionEvents"/> interface.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.System)]
internal interface ISolutionSource
{
    /// <summary>
    /// Provides access to the VS solution object.
    /// </summary>
    /// <remarks>
    /// Must be called from the main thread.
    /// </remarks>
    IVsSolution Solution { get; }

    /// <summary>
    /// Creates a new subscription for solution events that will call back via <paramref name="eventListener" />.
    /// </summary>
    /// <param name="eventListener">The callback for events. Note that it may also implement additional version(s) of this interface.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An object that unsubscribes when disposed.</returns>
    Task<IAsyncDisposable> SubscribeAsync(IVsSolutionEvents eventListener, CancellationToken cancellationToken = default);
}
