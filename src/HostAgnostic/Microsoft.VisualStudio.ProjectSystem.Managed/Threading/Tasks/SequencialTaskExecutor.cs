// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    /// Runs tasks in the sequence they are added. This is done by starting with a completed task, and leveraging ContinueWith\Unwrap to 
    /// "schedule" subsequent tasks to start after the previous one is completed. The Task containing the callers function is returned so that
    /// the caller can await for their specific task to complete. When disposed unprocessed tasks are cancelled.
    /// </summary>
    internal class SequencialTaskExecutor : IDisposable
    {
        private bool _disposed;
        private Task _taskAdded = Task.CompletedTask;
        private readonly object _syncObject = new object();
        private CancellationTokenSource _disposedCancelTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Deadlocks will occur if a task returned from ExecuteTask , awaits a task which also calls ExcecuteTask. The 2nd one will never get started since
        /// it will be backed up behind the first one completing. The AysncLocal is used to detect when a task is being executed, and if a downstream one gets 
        /// added, it will be executed directly, rather than get queued
        /// </summary>
        private System.Threading.AsyncLocal<bool> _executingTask = new System.Threading.AsyncLocal<bool>();

        /// <summary>
        /// Adds a new task to the continuation chain and returns it so that it can be awaited. 
        /// </summary>
        public Task ExecuteTask(Func<Task> asyncFunction)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SequencialTaskExecutor));
            }

            lock (_syncObject)
            {
                // If we are on the same exceution chain, run the task directly
                if (_executingTask.Value)
                {
                    return asyncFunction();
                }

                _taskAdded = _taskAdded.ContinueWith(async (t) =>
                {
                    _disposedCancelTokenSource.Token.ThrowIfCancellationRequested();
                    try
                    {
                        _executingTask.Value = true;
                        await asyncFunction().ConfigureAwait(false);
                    }
                    finally
                    {
                        _executingTask.Value = false;
                    }
                }, TaskScheduler.Default).Unwrap();
            }
            return _taskAdded;
        }

        /// <summary>
        /// Dispose cancels outstanding tasks
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposedCancelTokenSource.Cancel();
                _disposed = true;
            }
        }
    }
}
