// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides extension methods for <see cref="IProjectAsynchronousTasksService"/> instances.
    /// </summary>
    internal static class ProjectAsynchronousTasksServiceExtensions
    {
        /// <summary>
        ///     Provides protection for the specified action that the project will not close before it has completed.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="asyncTaskService"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     Thrown if the project was already unloaded before this method was invoked.
        /// </exception>
        /// <remarks>
        ///     This method differs from the <see cref="CommonProjectSystemTools.LoadedProjectAsync(IProjectAsynchronousTasksService, Func{Task}, bool)"/> 
        ///     method, in that if the project is in the process of being unloaded by the time the action is executed, then it bails earlier by throwing 
        ///     <see cref="OperationCanceledException"/>.
        /// </remarks>
        public static JoinableTask LoadedProjectAvoidingUnnecessaryWorkAsync(this IProjectAsynchronousTasksService asyncTaskService, Func<Task> action)
        {
            Requires.NotNull(asyncTaskService, nameof(asyncTaskService));
            Requires.NotNull(action, nameof(action));

            return asyncTaskService.LoadedProjectAsync(() => {

                asyncTaskService.UnloadCancellationToken.ThrowIfCancellationRequested();

                return action();
            });
        }
    }
}
