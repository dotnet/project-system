// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio;

internal static class SynchronizationContextUtil
{
    /// <summary>
    /// Sets a <see langword="null"/> <see cref="SynchronizationContext"/>, and restores
    /// whatever context was <see cref="SynchronizationContext.Current"/> when disposed.
    /// </summary>
    /// <remarks>
    /// This method is intended for use in tests where we use JTF and test code that
    /// wants to switch to the main thread. Some of our tests don't actually construct
    /// JTF in a way that creates a main thread that can be switched to. For these cases,
    /// whatever thread creates the <see cref="Threading.JoinableTaskContext"/> is considered
    /// the main thread. In unit tests, it's likely this is a thread pool thread, and
    /// attempting to switch to the main thread will land on another thread pool thread.
    /// <see cref="Threading.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken)"/>
    /// attempts to validate the switch was successful, and throws when not. This causes test
    /// failures where we don't have a main thread to switch to. A workaround for this is
    /// to suppress the synchronization context, which disables the check and allows the test
    /// to pass.
    /// </remarks>
    /// <returns>An object that restores the previous synchronization context when disposed.</returns>
    public static IDisposable Suppress()
    {
        SynchronizationContext old = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(null);

        return new SuppressionReleaser(old);
    }

    private sealed class SuppressionReleaser(SynchronizationContext old) : IDisposable
    {
        private int _isDisposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) is 0)
            {
                SynchronizationContext.SetSynchronizationContext(old);
            }
        }
    }
}
