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
    internal sealed class SequentialTaskExecutor : IDisposable
    {
        private bool _disposed;
        private Task _taskAdded = Task.CompletedTask;
        private readonly object _syncObject = new object();
#pragma warning disable IDE0069 // Tests fail if this is disposed
        private readonly CancellationTokenSource _disposedCancelTokenSource = new CancellationTokenSource();
#pragma warning restore IDE0069 

        /// <summary>
        /// Deadlocks will occur if a task returned from ExecuteTask , awaits a task which also calls ExecuteTask. The 2nd one will never get started since
        /// it will be backed up behind the first one completing. The AsyncLocal is used to detect when a task is being executed, and if a downstream one gets
        /// added, it will be executed directly, rather than get queued
        /// </summary>
        private readonly System.Threading.AsyncLocal<bool> _executingTask = new System.Threading.AsyncLocal<bool>();

        /// <summary>
        /// Adds a new task to the continuation chain and returns it so that it can be awaited.
        /// </summary>
        public Task ExecuteTask(Func<Task> asyncFunction)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SequentialTaskExecutor));
            }

            lock (_syncObject)
            {
                // If we are on the same execution chain, run the task directly
                if (_executingTask.Value)
                {
                    return asyncFunction();
                }

                _taskAdded = _taskAdded.ContinueWith(async t =>
                {
                    _disposedCancelTokenSource.Token.ThrowIfCancellationRequested();
                    try
                    {
                        _executingTask.Value = true;
                        await asyncFunction();
                    }
                    finally
                    {
                        _executingTask.Value = false;
                    }
                }, TaskScheduler.Default).Unwrap();

                return _taskAdded;
            }
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
