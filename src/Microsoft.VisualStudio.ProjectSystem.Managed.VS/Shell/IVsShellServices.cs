// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    /// Provides common shell services in an agnostic manner
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Host)]
    internal interface IVsShellServices
    {
        /// <summary>
        /// Gets a value indicating whether VS is running in command line mode.
        /// </summary>
        /// <remarks>
        /// This has a free-threaded implementation.
        /// </remarks>
        Task<bool> IsCommandLineModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a value indicating whether VS is populating the solution cache during a command line operation.
        /// </summary>
        /// <remarks>
        /// Implies <see cref="IsCommandLineModeAsync"/> is also <see langword="true"/>.
        /// </remarks>
        /// <remarks>
        /// This has a free-threaded implementation.
        /// </remarks>
        Task<bool> IsPopulateSolutionCacheModeAsync(CancellationToken cancellationToken = default);
    }
}
