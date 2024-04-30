// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio;

internal static class SynchronizationContextUtil
{
    public static IDisposable Suppress()
    {
        SynchronizationContext old = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(null);

        return new SuppressionReleaser(old);
    }

    private sealed class SuppressionReleaser(SynchronizationContext old) : IDisposable
    {
        private SynchronizationContext? old = old;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref old, null) is { } restoreMe)
            {
                SynchronizationContext.SetSynchronizationContext(restoreMe);
            }
        }
    }
}
