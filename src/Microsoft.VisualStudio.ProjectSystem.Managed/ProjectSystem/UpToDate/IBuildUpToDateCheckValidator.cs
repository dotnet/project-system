// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private)]
    internal interface IBuildUpToDateCheckValidator
    {
        /// <summary>
        /// Validates that the project inputs and outputs are up-to-date with respect to one another.
        /// </summary>
        /// <remarks>
        /// This method is intended to determine whether the project is correctly up-to-date after build.
        /// The difference between this method and <see cref="IBuildUpToDateCheckProvider.IsUpToDateAsync"/> is
        /// that this method will not mutate any internal state. If in future that method is made idempotent, then
        /// this method (and probably the whole interface) could be removed.
        /// </remarks>
        /// <param name="cancellationToken">A token that is cancelled if the caller loses interest in the result.</param>
        /// <returns></returns>
        Task<(bool IsUpToDate, string? FailureReason, string? FailureDescription)> ValidateUpToDateAsync(CancellationToken cancellationToken = default);
    }
}
