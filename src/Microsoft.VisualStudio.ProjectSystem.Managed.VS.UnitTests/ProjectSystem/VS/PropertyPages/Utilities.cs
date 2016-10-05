// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal static class Utilities
    {

        // Checks operationCompleteFunc in a loop waiting for it to return true. Sleeps in 10ms chunks. If it times out
        // prior to operationCompleteFunc returning true, it throws a TimeoutException unless callbackOnFailure is specified.
        // The timeout is in milliseconds.
        public static void WaitForAsyncOperation(int timeout, Func<bool> operationCompleteFunc, Action callbackOnFailure = null)
        {
            long maxTime = timeout * 10000;
            long startingTicks = DateTime.UtcNow.Ticks;
            while(!operationCompleteFunc())
            {
                System.Threading.Thread.Sleep(10);
                if((DateTime.UtcNow.Ticks - startingTicks) > maxTime)
                {
                    if(callbackOnFailure != null)
                        callbackOnFailure();
                    else
                        throw new System.TimeoutException("Timed out waiting for operation to complete");
                }
            }
        }
    }
}
