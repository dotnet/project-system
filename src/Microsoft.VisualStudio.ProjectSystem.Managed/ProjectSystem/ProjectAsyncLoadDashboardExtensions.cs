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
        ///     Returns a task that completes when the host recognizes that the project has loaded, 
        ///     cancelling if the project is unloaded before it completes.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="dashboard"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="project"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The project was unloaded before project load had finished.
        /// </exception>
        public static Task ProjectLoadedInHostWithCancellation(this IProjectAsyncLoadDashboard dashboard, UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            return ProjectLoadedInHostWithCancellation(dashboard, project.Services.ProjectAsynchronousTasks);

        }

        /// <summary>
        ///     Returns a task that completes when the host recognizes that the project has loaded, 
        ///     cancelling if the project is unloaded before it completes.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="dashboard"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="tasksService"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The project was unloaded before project load had finished.
        /// </exception>
        public static Task ProjectLoadedInHostWithCancellation(this IProjectAsyncLoadDashboard dashboard, IProjectAsynchronousTasksService tasksService)
        {
            Requires.NotNull(dashboard, nameof(dashboard));
            Requires.NotNull(tasksService, nameof(tasksService));

            return dashboard.ProjectLoadedInHost.WithCancellation(tasksService.UnloadCancellationToken);
        }
    }
}
