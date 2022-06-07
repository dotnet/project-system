// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Interface through which components internal to the .NET Project System may interact with the fast up-to-date check.
    /// </summary>
    /// <remarks>
    /// The fast up-to-date check needs to know about build events for two reasons:
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
    /// Members of this interface are called by <c>UpToDateCheckBuildEventNotifier</c> in the VS layer.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = Composition.ImportCardinality.ExactlyOne)]
    internal interface IBuildUpToDateCheckProviderInternal
    {
        /// <summary>
        /// Notifies the fast up-to-date check that a build is starting.
        /// </summary>
        /// <remarks>
        /// Must also be called for rebuilds.
        /// </remarks>
        void NotifyBuildStarting(DateTime buildStartTimeUtc);

        /// <summary>
        /// Notifies the fast up-to-date check that a build completed, and whether it completed
        /// successfully or not.
        /// </summary>
        /// <remarks>
        /// Must also be called for rebuilds.
        /// </remarks>
        Task NotifyBuildCompletedAsync(bool wasSuccessful, bool isRebuild);
    }
}
