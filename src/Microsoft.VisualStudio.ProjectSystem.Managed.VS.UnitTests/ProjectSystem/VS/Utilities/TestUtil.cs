// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class TestUtil
    {
        /// <summary>
        /// Asynchronously runs the specified <paramref name="action"/> on an STA thread.
        /// </summary>
        public static Task RunStaTestAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object?>();

            var thread = new Thread(ThreadMethod);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;

            void ThreadMethod()
            {
                try
                {
                    action();

                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }
        }
    }
}
