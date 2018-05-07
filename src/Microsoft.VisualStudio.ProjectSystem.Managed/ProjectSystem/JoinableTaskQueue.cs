// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents a queue of <see cref="JoinableTask"/> instances 
    ///     that can awaited on safely from the UI thread.
    /// </summary>
    internal class JoinableTaskQueue
    {
        private readonly HashSet<JoinableTask> _joinableTasks = new HashSet<JoinableTask>();
        private readonly object _lock = new object();
        private readonly JoinableTaskContextNode _joinableTaskContextNode;

        public JoinableTaskQueue(JoinableTaskContextNode joinableTaskContextNode)
        {
            Requires.NotNull(joinableTaskContextNode, nameof(joinableTaskContextNode));

            _joinableTaskContextNode = joinableTaskContextNode;
        }

        public void Register(JoinableTask task)
        {
            Requires.NotNull(task, nameof(task));

            bool added;

            lock (_lock)
            {
                added = _joinableTasks.Add(task);
            }

            if (added)
            {
                // Set up a continuation that will cause this task to automatically be removed from our collection 
                // of tests when it is done executing, to avoid leaking memory of all the tasks ever registered.
                task.Task.ContinueWith(
                    (t, state) => ((JoinableTaskQueue)state).Unregister(task),
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        private void Unregister(JoinableTask joinableTask)
        {
            lock (_lock)
            {
                _joinableTasks.Remove(joinableTask);
            }
        }

        public async Task DrainAsync()
        {
            while (true)    // Keep draining until we're empty
            {
                JoinableTaskCollection joinableTasks = _joinableTaskContextNode.CreateCollection();
                joinableTasks.DisplayName = GetType().FullName;
                var tasks = new List<Task>();

                lock (_joinableTasks)
                {
                    foreach (JoinableTask joinableTask in _joinableTasks)
                    {
                        joinableTasks.Add(joinableTask);
                        tasks.Add(joinableTask.Task);
                    }
                }

                if (tasks.Count == 0)
                    break;

                // Let these tasks share the UI thread to avoid deadlocks in 
                // case they need it while its blocked on this method.
                using (joinableTasks.Join())
                {
                    await Task.WhenAll(tasks)
                              .ConfigureAwait(false);
                }
            }
        }
    }
}
