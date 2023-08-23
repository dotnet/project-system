// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

/// <summary>
/// Called when a solution build starts and completes.
/// </summary>
/// <remarks>
/// <para>
/// Members of this interface are called by <c>UpToDateCheckBuildEventNotifier</c> in the VS layer.
/// </para>
/// <para>
/// We track solution builds to reset various state in the up-to-date check, such as timestamp caches.
/// </para>
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface ISolutionBuildEventListener
{
    /// <summary>
    /// Called when a solution build is starting.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    void NotifySolutionBuildStarting();

    /// <summary>
    /// Called when the up-to-date check for a project completes.
    /// </summary>
    /// <remarks>
    /// Is only called for .NET Project System projects.
    /// </remarks>
    void NotifyProjectChecked(
        bool upToDate,
        bool? buildAccelerationEnabled,
        BuildAccelerationResult result,
        int configurationCount,
        int copyCount,
        int fileCount,
        TimeSpan waitTime,
        TimeSpan checkTime,
        LogLevel logLevel);

    /// <summary>
    /// Called when a project build is starting, after <see cref="NotifyProjectChecked"/>.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    void NotifyProjectBuildStarting(bool isRebuild);

    /// <summary>
    /// Called when a solution build completes.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    void NotifySolutionBuildCompleted(bool cancelled);
}
