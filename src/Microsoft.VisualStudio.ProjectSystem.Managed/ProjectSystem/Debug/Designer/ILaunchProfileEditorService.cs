// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug.Designer
{
    /// <summary>
    /// Creates <see cref="ILaunchProfileEditorConnection"/>s for CPS-based .NET projects.
    /// </summary>
    /// <remarks>
    /// When CPS proffers a brokered service that returns <see cref="ILaunchProfileEditorConnection"/>s
    /// it will delegate to the implementation of <see cref="ILaunchProfileEditorService"/>
    /// to satisfy the request.
    /// Some other component could presumably implement their own brokered service to return
    /// <see cref="ILaunchProfileEditorConnection"/>s and proffer it via a different service
    /// descriptor.
    /// </remarks>
    internal interface ILaunchProfileEditorService
    {
        /// <summary>
        /// Creates a connection to the launch profiles for the project specified by
        /// <paramref name="projectGuid"/>.
        /// </summary>
        /// <param name="projectGuid">The <see cref="Guid"/> uniquely identifying the desired project.</param>
        /// <param name="clientSession">
        /// The client object that will receive notifications from the server.
        /// Note that the server may invoke methods on this object before the call to
        /// <see cref="CreateConnectionAsync(Guid, ILaunchProfileEditorClientSession)" /> is complete.
        /// </param>
        /// <returns>
        /// An <see cref="ILaunchProfileEditorConnection"/> the client can use to update property
        /// values as well as signal when it is done (via calls to Dispose).
        /// </returns>
        Task<ILaunchProfileEditorConnection?> CreateConnectionAsync(Guid projectGuid, ILaunchProfileEditorClientSession clientSession);
    }
}
