// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    internal interface ITaskDelayScheduler : IDisposable
    {
        /// <summary>
        /// Schedules a task to be run. Note that the returning Task represents
        /// the current scheduled task but not necessarily represents the task that
        /// ends up doing the actual work. If another task is scheduled later which causes
        /// the cancellation of the current scheduled task, the caller will not know
        /// and need to use that latest return task instead.
        /// </summary>
        JoinableTask ScheduleAsyncTask(Func<CancellationToken, Task> asyncFnctionToCall);

        /// <summary>
        /// Returns true if updates are pending
        /// </summary>
        bool HasPendingUpdates { get; }

        /// <summary>
        /// Cancels any pending updates
        /// </summary>
        void CancelPendingUpdates();

        /// <summary>
        /// Holds the last scheduled task.
        /// </summary>
        JoinableTask LatestScheduledTask { get; }
    }
}
