// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

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
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = Composition.ImportCardinality.ExactlyOne)]
internal interface ISolutionBuildEventListener
{
    /// <summary>
    /// Called when a solution build is starting.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    void NotifySolutionBuildStarting(DateTime buildStartTimeUtc);

    /// <summary>
    /// Called when a solution build completes.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    void NotifySolutionBuildCompleted();
}
