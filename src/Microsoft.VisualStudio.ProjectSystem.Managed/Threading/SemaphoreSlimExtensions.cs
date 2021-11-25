// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Threading
{
    internal static partial class SemaphoreSlimExtensions
    {
        public static async Task<T> ExecuteWithinLockAsync<T>(this SemaphoreSlim semaphore, JoinableTaskCollection collection, JoinableTaskFactory factory, Func<Task<T>> task, CancellationToken cancellationToken = default)
        {
            // Join the caller to our collection, so that if the lock is already held by another task that needs UI 
            // thread access we don't deadlock if we're also being waited on by the UI thread. For example, when CPS
            // is draining critical tasks and is waiting us.
            using (collection.Join())
            {
                await semaphore.WaitAsync(cancellationToken);

                using (new SemaphoreDisposer(semaphore))
                {
                    // We do an inner JoinableTaskFactory.RunAsync here to workaround
                    // https://github.com/Microsoft/vs-threading/issues/132
                    JoinableTask<T> joinableTask = factory.RunAsync(task);

                    return await joinableTask.Task;
                }
            }
        }

        public static async Task ExecuteWithinLockAsync(this SemaphoreSlim semaphore, JoinableTaskCollection collection, JoinableTaskFactory factory, Func<Task> task, CancellationToken cancellationToken = default)
        {
            // Join the caller to our collection, so that if the lock is already held by another task that needs UI 
            // thread access we don't deadlock if we're also being waited on by the UI thread. For example, when CPS
            // is draining critical tasks and is waiting us.
            using (collection.Join())
            {
                await semaphore.WaitAsync(cancellationToken);

                using (new SemaphoreDisposer(semaphore))
                {
                    // We do an inner JoinableTaskFactory.RunAsync here to workaround
                    // https://github.com/Microsoft/vs-threading/issues/132
                    JoinableTask joinableTask = factory.RunAsync(task);

                    await joinableTask.Task;
                }
            }
        }

        public static async Task ExecuteWithinLockAsync(this SemaphoreSlim semaphore, JoinableTaskCollection collection, Action action, CancellationToken cancellationToken = default)
        {
            // Join the caller to our collection, so that if the lock is already held by another task that needs UI 
            // thread access we don't deadlock if we're also being waited on by the UI thread. For example, when CPS
            // is draining critical tasks and is waiting us.
            using (collection.Join())
            {
                await semaphore.WaitAsync(cancellationToken);

                using (new SemaphoreDisposer(semaphore))
                {
                    action();
                }
            }
        }

        public static async Task<T> ExecuteWithinLockAsync<T>(this SemaphoreSlim semaphore, JoinableTaskCollection collection, Func<T> func, CancellationToken cancellationToken = default)
        {
            // Join the caller to our collection, so that if the lock is already held by another task that needs UI 
            // thread access we don't deadlock if we're also being waited on by the UI thread. For example, when CPS
            // is draining critical tasks and is waiting us.
            using (collection.Join())
            {
                await semaphore.WaitAsync(cancellationToken);

                using (new SemaphoreDisposer(semaphore))
                {
                    return func();
                }
            }
        }
    }
}
