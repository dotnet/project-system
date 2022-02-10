// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Interface through which components internal to the .NET Project System may
    /// interact with the fast up-to-date check.
    /// </summary>
    internal interface IBuildUpToDateCheckProviderInternal
    {
        /// <summary>
        /// Notifies the fast up-to-date check that a rebuild is starting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An <see cref="IBuildUpToDateCheckProvider"/> is only invoked for builds, not for rebuilds.
        /// Our implementation of this check keeps track of the last build time. For our purposes, that
        /// includes the last rebuild time too.
        /// </para>
        /// <para>
        /// This method allows an external component (<c>BuildUpToDateCheckRebuildNotifier</c>, in the VS layer)
        /// to provide this information.
        /// </para>
        /// </remarks>
        void NotifyRebuildStarting();
    }
}
