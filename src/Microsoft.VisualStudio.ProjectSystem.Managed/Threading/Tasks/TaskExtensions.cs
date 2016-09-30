// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Returns a task which waits for the passed in task to complete or the timeout to expire. Throws
        /// a TimeoutExeption in that case. 
        /// </summary>
        public static async Task WaitForCompleteOrTimeout(this Task task, int timeoutInMilliseconds)
        {
            if (task != await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds)).ConfigureAwait(false))
            {
                // Throw the timeout message and let the caller decide on the appropriate handling
                throw new TimeoutException();
            }
        }
        /// <summary>
        /// Non throwing version return true if the task completed or false if a timeout
        /// </summary>
        public static async Task<bool> TryWaitForCompleteOrTimeout(this Task task, int timeoutInMilliseconds)
        {
            if (task != await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds)).ConfigureAwait(false))
            {
                return false;
            }
            return true;
        }
    }
}
