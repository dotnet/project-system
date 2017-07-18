// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading
{
    internal static partial class SemaphoreSlimExtensions
    {
        public static SemaphoreDisposer DisposableWait(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default(CancellationToken))
        {
            semaphore.Wait(cancellationToken);
            return new SemaphoreDisposer(semaphore);
        }

        public async static Task<SemaphoreDisposer> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default(CancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreDisposer(semaphore);
        }

        public static async Task<T> ExecuteWithinLockAsync<T>(this SemaphoreSlim semaphore, JoinableTaskCollection collection, JoinableTaskFactory factory, Func<Task<T>> task)
        {
            // Join the caller to our collection, so that if the lock is already held by another task that needs UI 
            // thread access we don't deadlock if we're also being waited on by the UI thread. For example, when CPS
            // is draining critical tasks and is waiting us.
            using (collection.Join())
            {
                using (await semaphore.DisposableWaitAsync().ConfigureAwait(false))
                {
                    // We do an inner JoinableTaskFactory.RunAsync here to workaround
                    // https://github.com/Microsoft/vs-threading/issues/132
                    JoinableTask<T> joinableTask = factory.RunAsync(task);

                    return await joinableTask.Task
                                             .ConfigureAwait(false);
                }
            }
        }

        public static async Task ExecuteWithinLockAsync(this SemaphoreSlim semaphore, JoinableTaskCollection collection, JoinableTaskFactory factory, Func<Task> task)
        {
            // Join the caller to our collection, so that if the lock is already held by another task that needs UI 
            // thread access we don't deadlock if we're also being waited on by the UI thread. For example, when CPS
            // is draining critical tasks and is waiting us.
            using (collection.Join())
            {
                using (await semaphore.DisposableWaitAsync().ConfigureAwait(false))
                {
                    // We do an inner JoinableTaskFactory.RunAsync here to workaround
                    // https://github.com/Microsoft/vs-threading/issues/132
                    JoinableTask joinableTask = factory.RunAsync(task);

                    await joinableTask.Task
                                      .ConfigureAwait(false);
                }
            }
        }
    }
}