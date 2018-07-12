// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Provides <see langword="static"/> extensions for <see cref="IProjectAsyncLoadDashboard"/> instances.
    /// </summary>
    internal static class ProjectAsyncLoadDashboardExtensions
    {
        /// <summary>
        /// Gets a task that completes when the host recognizes that this project is loaded.
        /// The returned task is cancelled if the project is unloaded before it completes.
        /// This extension should be used instead of waiting on ProjectLoadedInHost directly.
        /// </summary>
        public static Task ProjectLoadedInHostWithCancellation(
            this IProjectAsyncLoadDashboard dashboard, UnconfiguredProject project)
            => dashboard.ProjectLoadedInHost.WithCancellation(
                project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);
    }
}
