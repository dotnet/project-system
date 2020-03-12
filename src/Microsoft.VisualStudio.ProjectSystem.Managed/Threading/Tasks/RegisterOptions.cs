// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    ///     Provides options for <see cref="CancellationTokenExtensions.Register(CancellationToken, RegisterOptions, Action)"/>.
    /// </summary>
    internal enum RegisterOptions
    {
        /// <summary>
        ///     Throw <see cref="ObjectDisposedException"/> if the <see cref="CancellationToken"/> is already disposed.
        /// </summary>
        None,

        /// <summary>
        ///     Execute the specified callback immediately if the <see cref="CancellationToken"/> is already canceled and disposed.
        /// </summary>
        ExecuteImmediatelyIfAlreadyCanceledAndDisposed,
    }
}
