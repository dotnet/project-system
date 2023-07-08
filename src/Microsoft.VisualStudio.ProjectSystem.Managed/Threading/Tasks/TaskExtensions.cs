// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    ///     Provides extensions for <see cref="Task"/> instances.
    /// </summary>
    internal static class TaskExtensions
    {
        /// <summary>
        ///     Creates a task that will complete indicating whether the specified task completed or timed-out.
        /// </summary>
        /// <param name="task">
        ///     The <see cref="Task"/> to wait on for completion.
        /// </param>
        /// <param name="millisecondsTimeout">
        ///     The number of milliseconds to wait, or <see cref="Timeout.Infinite"/> (-1) to wait indefinitely.
        /// </param>
        /// <returns>
        ///     An <see cref="Task"/> instance on which to wait.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="millisecondsTimeout"/> is a negative number other than -1, which represents an infinite time-out.
        /// </exception>
        /// <exception cref="TaskCanceledException">
        ///     <paramref name="task"/> is cancelled.
        /// </exception>
        public static async Task<bool> TryWaitForCompleteOrTimeoutAsync(this Task task, int millisecondsTimeout)
        {
            if (millisecondsTimeout == Timeout.Infinite)
            {
                await task;
                return true;
            }

            using var cts = new CancellationTokenSource();

            var delay = Task.Delay(millisecondsTimeout, cts.Token);

            if (ReferenceEquals(delay, await Task.WhenAny(task, delay)))
            {
                return false;
            }

            cts.Cancel();
            await task;
            return true;
        }
    }
}
