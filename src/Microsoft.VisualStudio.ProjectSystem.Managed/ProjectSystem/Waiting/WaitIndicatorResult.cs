// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
