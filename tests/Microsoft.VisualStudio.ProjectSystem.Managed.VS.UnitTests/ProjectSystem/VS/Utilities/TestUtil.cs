// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class TestUtil
    {
        /// <summary>
        /// Asynchronously runs the specified <paramref name="action"/> on an STA thread.
        /// </summary>
        public static Task RunStaTestAsync(Action action)
        {
            var tcs = new TaskCompletionSource();

            var thread = new Thread(ThreadMethod);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;

            void ThreadMethod()
            {
                try
                {
                    action();

                    tcs.SetResult();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }
        }
    }
}
