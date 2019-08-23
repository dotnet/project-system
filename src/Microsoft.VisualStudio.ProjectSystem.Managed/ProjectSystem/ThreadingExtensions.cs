// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides extensions for <see cref="IProjectThreadingService"/>.
    /// </summary>
    internal static class ThreadingExtensions
    {
        /// <summary>
        ///     Gets an awaitable whose completion will execute on the UI thread, mitigating deadlocks and unwanted reentrancy.
        /// </summary>
        /// <param name="threading">
        ///     The <see cref="IProjectThreadingService"/> containing the <see cref="JoinableTaskFactory"/> to use.
        /// </param>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="threading"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and <paramref name="cancellationToken"/> is cancelled.
        /// </exception>
        public static JoinableTaskFactory.MainThreadAwaitable SwitchToUIThread(this IProjectThreadingService threading, CancellationToken cancellationToken)
        {
            Requires.NotNull(threading, nameof(threading));

            return threading.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}
