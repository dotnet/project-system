// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    /// <summary>
    /// Extension point for components that report on incremental build failures.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal interface IIncrementalBuildFailureReporter
    {
        /// <summary>
        ///   Gets whether this reporter is enabled or not.
        /// </summary>
        /// <remarks>
        ///   When this method returns <see langword="false"/>, <see cref="ReportFailureAsync(string, string, TimeSpan, CancellationToken)"/>
        ///   will not be called. If all exports of <see cref="IIncrementalBuildFailureReporter"/> return false, the incremental
        ///   build failure check is not performed at all.
        /// </remarks>
        /// <param name="cancellationToken">A token whose cancellation marks lost interest in the result of this task.</param>
        /// <returns>A task that resolves to <see langword="true"/> if this reporter is currently enabled, otherwise <see langword="false"/>.</returns>
        Task<bool> IsEnabledAsync(CancellationToken cancellationToken);

        /// <summary>
        ///   Reports an incremental build failure for the project in the current scope.
        /// </summary>
        /// <param name="failureReason">A string that identifies the reason for the failure.</param>
        /// <param name="failureDescription">A detailed description of the failure. May include file paths and timestamps.</param>
        /// <param name="checkDuration">The duration spent performing the incremental build failure check.</param>
        /// <param name="cancellationToken">A token whose cancellation marks lost interest in the result of this task.</param>
        /// <returns>A task that completes when reporting is done.</returns>
        Task ReportFailureAsync(string failureReason, string failureDescription, TimeSpan checkDuration, CancellationToken cancellationToken);
    }
}
