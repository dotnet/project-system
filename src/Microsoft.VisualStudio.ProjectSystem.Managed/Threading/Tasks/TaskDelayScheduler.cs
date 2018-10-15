// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.ProjectSystem;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    /// TaskDelayScheduler
    ///
    /// Helper class which allows a task to be scheduled to run after some delay, but if a new task
    /// is scheduled before the delay runs out, the previous task is cancelled.
    /// </summary>
    internal sealed class TaskDelayScheduler : ITaskDelayScheduler
    {
        private readonly object _syncObject = new object();
        private readonly TimeSpan _taskDelayTime;
        private readonly CancellationToken _originalSourceToken;
        private readonly IProjectThreadingService _threadingService;

        // Task completion source for cancelling a pending task
        private CancellationTokenSource PendingUpdateTokenSource { get; set; }

        // Holds the latest scheduled task
        public JoinableTask LatestScheduledTask { get; private set; }

        /// <summary>
        /// Creates an instance of the TaskDelayScheduler. If an originalSourceToken is passed, it will be linked to the PendingUpdateTokenSource so
        /// that cancelling that token will also flow through and cancel a pending update.
        /// </summary>
        public TaskDelayScheduler(TimeSpan taskDelayTime, IProjectThreadingService threadService, CancellationToken originalSourceToken)
        {
            _taskDelayTime = taskDelayTime;
            _originalSourceToken = originalSourceToken;
            _threadingService = threadService;
        }

        /// <summary>
        /// Schedules a task to be run. Note that the returning Task represents
        /// the current scheduled task but not necessarily represents the task that
        /// ends up doing the actual work. If another task is scheduled later which causes
        /// the cancellation of the current scheduled task, the caller will not know
        /// and need to use that latest returned task instead.
        /// </summary>
        public JoinableTask ScheduleAsyncTask(Func<CancellationToken, Task> asyncFunctionToCall)
        {
            lock (_syncObject)
            {
                // A new submission is being requested to be scheduled, cancel previous
                // submissions first.
                ClearPendingUpdates(cancel: true);

                PendingUpdateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_originalSourceToken);
                CancellationToken token = PendingUpdateTokenSource.Token;

                // We want to return a joinable task so wrap the function
                LatestScheduledTask = _threadingService.JoinableTaskFactory.RunAsync(() => ThrottleAsync(asyncFunctionToCall, token));
                return LatestScheduledTask;
            }
        }

        private async Task ThrottleAsync(Func<CancellationToken, Task> asyncFunctionToCall, CancellationToken token)
        {
            try
            {
                // First we wait the delay time. If another request has been made in the interval, then this task
                // is cancelled.
                try
                {
                    if (!token.IsCancellationRequested)
                    {
                        await Task.Delay(_taskDelayTime, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // fall through
                }

                bool isCanceled = token.IsCancellationRequested;
                lock (_syncObject)
                {
                    // We want to clear any existing cancellation token IF it matches our token
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
            await asyncFunctionToCall(token);
        }

        /// <summary>
        /// Clears the PendingUpdateTokenSource and if cancel is true cancels the token.
        /// </summary>
        /// <remarks>
        /// Callers must lock <see cref="_syncObject"/> before calling this method.
        /// </remarks>
        private void ClearPendingUpdates(bool cancel)
        {
            CancellationTokenSource cts = PendingUpdateTokenSource;

            if (cts != null)
            {
                PendingUpdateTokenSource = null;

                // Cancel any previously scheduled processing if requested
                if (cancel)
                {
                    cts.Cancel();
                }

                cts.Dispose();
            }
        }

        /// <summary>
        /// Cancels any pending tasks
        /// </summary>
        public void Dispose()
        {
            lock (_syncObject)
            {
                ClearPendingUpdates(cancel: true);
            }
        }

        /// <summary>
        /// Mechanism that owners can use to cancel pending tasks.
        /// </summary>
        public void CancelPendingUpdates()
        {
            lock (_syncObject)
            {
                ClearPendingUpdates(cancel: true);
            }
        }
    }
}
