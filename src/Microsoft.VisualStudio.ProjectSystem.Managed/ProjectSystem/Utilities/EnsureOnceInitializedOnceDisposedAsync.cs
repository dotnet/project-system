// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal abstract class EnsureOnceInitializedOnceDisposedAsync : OnceInitializedOnceDisposedAsync
    {
        protected EnsureOnceInitializedOnceDisposedAsync(JoinableTaskContextNode joinableTaskContextNode) : base(joinableTaskContextNode) { }

        private int _isInitialized;

        protected new Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _isInitialized, 1) == 0)
            {
                return base.InitializeAsync();
            }

            return Task.CompletedTask;
        }
    }
}
