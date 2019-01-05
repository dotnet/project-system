// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
