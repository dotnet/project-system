// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace System.Threading.Tasks
{
    public static class TaskTimeoutExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeout, cts.Token);
            Task completedTask = await Task.WhenAny(task, timeoutTask);

            if (timeoutTask == completedTask)
                throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds} ms");

            cts.Cancel();
            await task;
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeout, cts.Token);
            Task completedTask = await Task.WhenAny(task, timeoutTask);

            if (timeoutTask == completedTask)
                throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds} ms");

            cts.Cancel();
            return await task;
        }
    }
}
