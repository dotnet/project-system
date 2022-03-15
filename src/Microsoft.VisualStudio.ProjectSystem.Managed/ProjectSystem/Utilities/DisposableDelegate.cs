// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
