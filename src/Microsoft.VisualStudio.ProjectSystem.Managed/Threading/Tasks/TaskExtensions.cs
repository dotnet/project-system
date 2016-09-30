// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    internal static class TaskExtensions
    {
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
