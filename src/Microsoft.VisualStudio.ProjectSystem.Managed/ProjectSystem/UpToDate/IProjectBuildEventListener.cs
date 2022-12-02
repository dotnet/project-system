// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <summary>
/// Called (in a specific project configuration) when that project's build starts and completes.
/// </summary>
/// <remarks>
/// <para>
/// Members of this interface are called by <c>UpToDateCheckBuildEventNotifier</c> in the VS layer.
/// </para>
/// <para>
/// Methods here are called by VS when the project's build starts and ends. We need this for two reasons:
/// </para>
/// <list type="number">
///     <item>
///         An <see cref="IBuildUpToDateCheckProvider"/> is only invoked for builds, not for rebuilds. We need
///         to know when rebuilds occur.
///     </item>
///     <item>
///         A call to <see cref="IBuildUpToDateCheckProvider"/> does not necessarily guarantee a build will occur.
///         We need to know the time at which the last successful build occurred.
///     </item>
/// </list>
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = Composition.ImportCardinality.ExactlyOne)]
internal interface IProjectBuildEventListener
{
    /// <summary>
    /// Called when this project's build starts.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    void NotifyBuildStarting(DateTime buildStartTimeUtc);

    /// <summary>
    /// Called when this project's build completes.
    /// </summary>
    /// <remarks>
    /// Must be called for both builds for rebuilds.
    /// </remarks>
    Task NotifyBuildCompletedAsync(bool wasSuccessful, bool isRebuild);
}
