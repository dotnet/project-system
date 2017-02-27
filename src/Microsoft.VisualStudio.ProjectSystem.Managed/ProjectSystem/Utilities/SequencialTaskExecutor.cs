// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Runs tasks in the sequence they are added. This is done by starting with a completed task, and leveraging ContinueWith\Unwrap to 
    /// "schedule" subsequent tasks to start after the previous one is completed. The Task continaing the callers function is returned so that
    /// the caller can await for their specific task to complete. When disposed unprocessed tasks are cancelled.
    /// </summary>
    internal class SequencialTaskExecutor : IDisposable 
    {
        private bool Disposed;
        private Task TaskAdded = Task.CompletedTask;
        private object SyncObject = new object();
        private CancellationTokenSource DisposedCancelTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Adds a new task to the continuation chain and returns it so that it can be awaited. 
        /// Note that deadlocks will occur if a task returned from this function, awaits a task which also calls this function as that task will not 
        /// start until this one ccompletes. 
        /// </summary>
        public Task ExecuteTask(Func<Task> asyncFunction)
        {
            if(Disposed)
            {
                throw new ObjectDisposedException(nameof(SequencialTaskExecutor));
            }

            lock(SyncObject)
            {
                TaskAdded = TaskAdded.ContinueWith(async (t) => 
                { 
                    DisposedCancelTokenSource.Token.ThrowIfCancellationRequested();
                    await asyncFunction().ConfigureAwait(false);
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
