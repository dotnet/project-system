// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Threading;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{

    /// <summary>
    /// TaskDelayScheduler
    ///
    /// Helper class which allows a task to be scheduled to run after some delay, but if a new task
    /// is scheduled before the delay runs out, the previous task is cancelled.
    /// </summary>
    internal sealed class TaskDelayScheduler : ITaskDelayScheduler
    {
        private object SyncObject = new object();
        private readonly IProjectThreadingService _threadingService;

        // Delay time can be adjusted after creation. Won't affect any pending task
        public TimeSpan TaskDelayTime { get; set; }

        // Task completetion source for cancelling a pending file update.
        private CancellationTokenSource PendingUpdateTokenSource { get; set; }

        private CancellationToken OriginalSourceToken { get; set; }

        // True if there are pending file changes
        public bool HasPendingUpdates { get { return PendingUpdateTokenSource != null; } }

        // Holds the latest scheduled task
        public JoinableTask LatestScheduledTask { get; private set; }

        /// <summary>
        /// Creates an instance of the TaskDelayScheduler. If an originalSourceToken is passed, it will be linked to the PendingUpdateTokenSource so
        /// that cancelling that token will also flow through and cancel a pending update.
        /// </summary>
        public TaskDelayScheduler(TimeSpan taskDelayTime, IProjectThreadingService threadService, CancellationToken originalSourceToken)
        {
            TaskDelayTime = taskDelayTime;
            OriginalSourceToken = originalSourceToken;
            _threadingService = threadService;
        }

        /// <summary>
        /// Schedules a task to be run. Note that the returning Task represents
        /// the current scheduled task but not necessarily represents the task that
        /// ends up doing the actual work. If another task is scheduled later which causes
        /// the cancellation of the current scheduled task, the caller will not know
        /// and need to use that latest returned task instead.
        /// </summary>
        public JoinableTask ScheduleAsyncTask(Func<CancellationToken, Task> asyncFnctionToCall)
        {
            lock (this.SyncObject)
            {
                // A new submission is being requested to be scheduled, cancel previous
                // submissions first.
                ClearPendingUpdates(cancel: true);

                PendingUpdateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(OriginalSourceToken);
                var token = PendingUpdateTokenSource.Token;

                // We want to return a joinable task so wrap the function
                LatestScheduledTask = _threadingService.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThrottleAsync(asyncFnctionToCall, token).ConfigureAwait(false);
                });
                return LatestScheduledTask;
            }
        }

        private async Task ThrottleAsync(Func<CancellationToken, Task> asyncFnctionToCall, CancellationToken token)
        {
            try
            {
                // First we wait the delay time. If another request has been made in the interval, then this task
                // is cancelled. To avoid unnecessary OperationCanceled exceptions it tests to see if the token has
                // been canceled
                await Task.Delay(TaskDelayTime).ConfigureAwait(true);

                bool isCanceled = token.IsCancellationRequested;
                lock (this.SyncObject)
                {
                    // We want to clear any existing cancelation token IF it matches our token
                    if (PendingUpdateTokenSource != null && PendingUpdateTokenSource.Token == token)
                    {
                        ClearPendingUpdates(cancel: false);
                    }
                }

                if (isCanceled)
                {
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // Sometimes the CTS from which the token was obtained is canceled and disposed of
                // while we're still running this task. But it's OK, it basically means this task shouldn't 
                // be running any more. There is no point throwing a canceled exception
                return;
            }

            // Execute the code
            await asyncFnctionToCall(token).ConfigureAwait(true);
        }

        /// <summary>
        /// Clears the PendingUpdateTokenSource and if cancel is true cancels the token
        /// </summary>
        private void ClearPendingUpdates(bool cancel)
        {
            lock (this.SyncObject)
            {
                if (PendingUpdateTokenSource != null)
                {
                    // Cancel any previously scheduled processing if requested
                    if (cancel)
                    {
                        PendingUpdateTokenSource.Cancel();
                    }
                    var cts = PendingUpdateTokenSource;
                    PendingUpdateTokenSource = null;
                    cts.Dispose();
                }
            }
        }

        /// <summary>
        /// Cancels any pending tasks
        /// </summary>
        public void Dispose()
        {
            ClearPendingUpdates(true);
        }

        /// <summary>
        /// Mechanism that owners can use to cancel pending tasks.
        /// </summary>
        public void CancelPendingUpdates()
        {
            ClearPendingUpdates(true);
        }
    }
}
