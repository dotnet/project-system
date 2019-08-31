// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information. 

using System;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Wraps an <see cref="Action"/> delegate as an <see cref="IDisposable"/>, ensuring it is called
    /// only once, the first time <see cref="Dispose"/> is called.
    /// </summary>
    internal sealed class DisposableDelegate : IDisposable
    {
        private Action? _onDispose;

        public DisposableDelegate(Action onDispose)
        {
            Requires.NotNull(onDispose, nameof(onDispose));

            _onDispose = onDispose;
        }

        public void Dispose()
        {
            // Prevent double-dispose, and null field out on dispose
            Action? action = Interlocked.Exchange(ref _onDispose, null);

            action?.Invoke();
        }
    }
}
