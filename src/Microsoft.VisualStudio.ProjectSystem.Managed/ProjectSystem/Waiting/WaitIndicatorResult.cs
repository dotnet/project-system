// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    internal enum WaitIndicatorResult
    {
        Completed,
        Canceled,
    }

    internal static class WaitIndicatorResultExtensions
    {
        public static bool WasCanceled(this WaitIndicatorResult result) => result == WaitIndicatorResult.Canceled;
    }
}
