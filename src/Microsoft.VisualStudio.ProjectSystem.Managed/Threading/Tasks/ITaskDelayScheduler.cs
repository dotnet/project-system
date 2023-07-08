// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Threading.Tasks
{
    internal interface ITaskDelayScheduler : IDisposable
    {
        /// <summary>
        /// Schedules an asynchronous operation to be run after some delay, acting as a trailing-edge debouncer.
        /// Subsequent scheduled operations will cancel previously scheduled tasks.
        /// </summary>
        /// <remarks>
        /// <para>Operations can overlap, however the <see cref="CancellationToken"/> passed to an earlier
        /// operation is cancelled if and when a later operation is scheduled, which always occurs before that
        /// later operation is started. It is up to the caller to ensure proper use of the cancellation token
        /// provided when <paramref name="operation"/> is invoked.</para>
        ///
        /// <para>
        /// The returned Task represents
        /// the current scheduled task but not necessarily represents the task that
        /// ends up doing the actual work. If another task is scheduled later which causes
        /// the cancellation of the current scheduled task, the caller will not know
        /// and need to use that latest return task instead.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        JoinableTask ScheduleAsyncTask(Func<CancellationToken, Task> operation, CancellationToken token = default);
    }
}
