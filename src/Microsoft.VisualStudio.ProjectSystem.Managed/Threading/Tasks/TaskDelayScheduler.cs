// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.ProjectSystem;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    /// Helper class which allows a task to be scheduled to run after some delay, but if a new task
    /// is scheduled before the delay runs out, the previous task is cancelled.
    /// </summary>
    internal sealed class TaskDelayScheduler : ITaskDelayScheduler
    {
        private readonly TimeSpan _taskDelayTime;
        private readonly IProjectThreadingService _threadingService;
        private readonly CancellationSeries _cancellationSeries;

        /// <summary>
        /// Creates an instance of the TaskDelayScheduler. If an originalSourceToken is passed, it will be linked to the PendingUpdateTokenSource so
        /// that cancelling that token will also flow through and cancel a pending update.
        /// </summary>
        public TaskDelayScheduler(TimeSpan taskDelayTime, IProjectThreadingService threadService, CancellationToken originalSourceToken)
        {
            _taskDelayTime = taskDelayTime;
            _threadingService = threadService;
            _cancellationSeries = new CancellationSeries(originalSourceToken);
        }

        /// <inheritdoc />
        public JoinableTask ScheduleAsyncTask(Func<CancellationToken, Task> operation, CancellationToken token = default)
        {
            CancellationToken nextToken = _cancellationSeries.CreateNext(token);

            // We want to return a joinable task so wrap the function
            return _threadingService.JoinableTaskFactory.RunAsync(async () =>
            {
                if (nextToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await Task.Delay(_taskDelayTime, nextToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (!nextToken.IsCancellationRequested)
                {
                    await operation(nextToken);
                }
            });
        }

        /// <summary>
        /// Cancels any pending tasks and disposes this object.
        /// </summary>
        public void Dispose()
        {
            _cancellationSeries.Dispose();
        }
    }
}
