// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<Task> _callback;

        public AsyncDisposable(Func<Task> callback) => _callback = callback;

        public async ValueTask DisposeAsync() => await _callback();
    }
}
