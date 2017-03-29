// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Runs tasks in the sequence they are added. This is done by starting with a completed task, and leveraging ContinueWith\Unwrap to 
    /// "schedule" subsequent tasks to start after the previous one is completed. The Task containing the callers function is returned so that
    /// the caller can await for their specific task to complete. When disposed unprocessed tasks are cancelled.
    /// </summary>
    internal class SequencialTaskExecutor : IDisposable 
    {
        private bool Disposed;
        private Task TaskAdded = Task.CompletedTask;
        private object SyncObject = new object();
        private CancellationTokenSource DisposedCancelTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Deadlocks will occur if a task returned from ExecuteTask , awaits a task which also calls ExcecuteTask. The 2nd one will never get started since
        /// it will be backed up behind the first one completing. The AysncLocal is used to detect when a task is being executed, and if a downstream one gets 
        /// added, it will be executed directly, rather than get queued
        /// </summary>
        private AsyncLocal<bool> _executingTask = new AsyncLocal<bool>();

        /// <summary>
        /// Adds a new task to the continuation chain and returns it so that it can be awaited. 
        /// </summary>
        public Task ExecuteTask(Func<Task> asyncFunction)
        {
            if(Disposed)
            {
                throw new ObjectDisposedException(nameof(SequencialTaskExecutor));
            }

            lock(SyncObject)
            {
                // If we are on the same exceution chain, run the task directly
                if (_executingTask.Value)
                {
                    return asyncFunction();
                }

                TaskAdded = TaskAdded.ContinueWith(async (t) => 
                { 
                    DisposedCancelTokenSource.Token.ThrowIfCancellationRequested();
                    try
                    {
                        _executingTask.Value = true;
                        await asyncFunction().ConfigureAwait(false);
                    }
                    finally
                    {
                        _executingTask.Value = false;
                    }
                },TaskScheduler.Default).Unwrap();
            }
            return TaskAdded;
        }

        /// <summary>
        /// Dispose cancels outstanding tasks
        /// </summary>
        public void Dispose()
        {
            if(!Disposed)
            {
                DisposedCancelTokenSource.Cancel();
                Disposed = true;
            }
        }
    }
}
