// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace System.Threading.Tasks
{
    public static class TaskTimeoutExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var timeoutTask = Task.Delay(timeout, cts.Token);
                Task completedTask = await Task.WhenAny(task, timeoutTask);

                if (timeoutTask == completedTask)
                    throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds} ms");

                cts.Cancel();
                await task;
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var timeoutTask = Task.Delay(timeout, cts.Token);
                Task completedTask = await Task.WhenAny(task, timeoutTask);

                if (timeoutTask == completedTask)
                    throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds} ms");

                cts.Cancel();
                return await task;
            }
        }
    }
}
