// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<ValueTask> _callback;

        public AsyncDisposable(Func<ValueTask> callback) => _callback = callback;

        public async ValueTask DisposeAsync() => await _callback();
    }
}
