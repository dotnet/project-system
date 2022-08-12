// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    /// Provides common shell services in an agnostic manner
    /// </summary>
    /// <remarks>
    /// This contract defines the boundary between the VS shell system
    /// and the consumer to help avoid taking unnecessary assembly dependencies
    /// in the client.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Host)]
    internal interface IVsShellServices
    {
        /// <summary>
        /// Gets a value indicating whether VS is running in the server mode.
        /// </summary>
        /// <remarks>
        /// This has a free-threaded implementation.
        /// </remarks>
        bool IsInServerMode { get; }
    }
}
