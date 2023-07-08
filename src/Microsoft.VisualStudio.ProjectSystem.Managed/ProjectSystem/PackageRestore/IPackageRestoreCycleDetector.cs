// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
/// Defines a component that can detect cyclic behavior during NuGet package restore operations.
/// </summary>
/// <remarks>
/// <para>
/// Restore operations can change the project in a way that requires a subsequent design-time build.
/// That build may conclude that another restore is required. Although uncommon, it is possible to
/// author a project such that this process cycles indefinitely, usually toggling between two states.
/// </para>
/// <para>
/// In such a situation, without this cycle detection the IDE would perform background restores and builds
/// indefinitely, burning CPU, reducing responsiveness and wasting power/battery.
/// 
/// The intention is that the caller will cancel the next restore when a cycle is detected, thus breaking the cycle.
/// </para>
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
internal interface IPackageRestoreCycleDetector
{
    /// <summary>
    /// Detects whether a restore cycle has occurred.
    /// </summary>
    /// <remarks>
    /// The implementation may take some action internally, such as alerting the user and/or sending telemetry.
    /// The caller is expected to use a return value of <see langword="true"/> to halt further restores from
    /// occurring and consuming resources indefinitely.
    /// </remarks>
    /// <param name="hash">The most recent restore hash value, computed from all inputs to the restore operation.</param>
    /// <param name="cancellationToken">A token that can signal a loss of interest in the result.</param>
    /// <returns><see langword="true"/> if a cycle is detected, otherwise <see langword="false"/>.</returns>
    Task<bool> IsCycleDetectedAsync(Hash hash, CancellationToken cancellationToken);
}
