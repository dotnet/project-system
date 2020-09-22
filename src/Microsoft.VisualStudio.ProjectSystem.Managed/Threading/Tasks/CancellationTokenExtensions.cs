// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    ///     Provides static extensions methods for <see cref="CancellationToken"/> instances.
    /// </summary>
    internal static class CancellationTokenExtensions
    {
        /// <summary>
        ///     Registers a delegate that will be called when this <see cref="CancellationToken"/>
        ///     is canceled, executing the callback immediately if it has already been canceled
        ///     and <see cref="RegisterOptions.ExecuteImmediatelyIfAlreadyCanceledAndDisposed"/> is specified.
        /// </summary>
        /// <param name="token">
        ///     The <see cref="CancellationToken"/> to register for cancellation.
        /// </param>
        /// <param name="options">
        ///     Options that control how the registration occurs.
        /// </param>
        /// <param name="callback">
        ///     The delegate to be executed when the <see cref="CancellationToken"/> is canceled.
        /// </param>
        /// <returns>
        ///     A <see cref="CancellationTokenRegistration"/> that can be used to deregister the callback.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="callback"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     <paramref name="options"/> is <see cref="RegisterOptions.None"/> and the <see cref="CancellationTokenSource"/>
        ///     has been disposed.
        /// </exception>
        internal static CancellationTokenRegistration Register(this CancellationToken token, RegisterOptions options, Action callback)
        {
            Requires.NotNull(callback, nameof(callback));

            try
            {
                return token.Register(callback);
            }
            catch (ObjectDisposedException) when (options == RegisterOptions.ExecuteImmediatelyIfAlreadyCanceledAndDisposed)
            {
                // The CancellationTokenSource has already been disposed.  It rejected the register.
                // But now we know the CancellationToken is in its final state (either canceled or not).
                // So simulate the right behavior by invoking the callback or not, based on whether it was
                // already canceled.
                if (token.IsCancellationRequested)
                {
                    callback();
                }

                return new CancellationTokenRegistration();
            }
        }
    }
}
