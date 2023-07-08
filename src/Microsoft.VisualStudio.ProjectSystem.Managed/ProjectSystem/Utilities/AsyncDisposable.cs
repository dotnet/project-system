// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<ValueTask> _callback;

        private int _isDisposed;

        public AsyncDisposable(Func<ValueTask> callback) => _callback = callback;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                await _callback();
            }
        }
    }
}
