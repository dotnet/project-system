// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    /// Produces a series of <see cref="CancellationToken"/> objects such that requesting a new token
    /// causes the previously issued token to be cancelled.
    /// </summary>
    /// <remarks>
    /// <para>Consuming code is responsible for managing overlapping asynchronous operations.</para>
    /// <para>This class has a lock-free implementation to minimise latency and contention.</para>
    /// </remarks>
    public sealed class CancellationSeries : IDisposable
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly CancellationToken[] _superToken;

        /// <summary>
        /// Initializes a new instance of <see cref="CancellationSeries"/>.
        /// </summary>
        /// <param name="token">An optional cancellation token that, when cancelled, cancels the last
        /// issued token and causes any subsequent tokens to be issued in a cancelled state.</param>
        public CancellationSeries(CancellationToken token = default)
        {
            _superToken = token == default 
                ? null 
                : new[] { token };
        }

        /// <summary>
        /// Gets the next <see cref="CancellationToken"/> in the series, ensuring the last issued
        /// token (if any) is cancelled before this method returns.
        /// </summary>
        /// <param name="token">An optional cancellation token that, when cancelled, cancels the
        /// returned token.</param>
        /// <returns>
        /// A cancellation token that will be cancelled when either:
        /// <list type="bullet">
        /// <item><see cref="GetToken"/> is called again</item>
        /// <item>The token passed to this method (if any) is cancelled</item>
        /// <item>The token passed to the constructor (if any) is cancelled</item>
        /// <item><see cref="Dispose"/> is called</item>
        /// </list>
        /// </returns>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        public CancellationToken GetToken(CancellationToken token = default)
        {
            // Create the next CTS. There are four scenarios, depending upon whether
            // a super token was given to the constructor, and whether a token was
            // passed to this method.

            CancellationTokenSource nextSource = _superToken == null
                ? token == default
                    ? new CancellationTokenSource()
                    : CancellationTokenSource.CreateLinkedTokenSource(token)
                : token == default
                    ? CancellationTokenSource.CreateLinkedTokenSource(_superToken)
                    : CancellationTokenSource.CreateLinkedTokenSource(token, _superToken[0]);

            CancellationToken nextToken = nextSource.Token;
            CancellationTokenSource priorSource = Volatile.Read(ref _cts);

            // Loop until we succeed in swapping a known prior for our next.
            // This ensures a proper sequence is observed, which is important
            // as otherwise we may cancel a prior twice, which would coincide
            // with a prior not being cancelled at all. We need a strict order
            // to maintain one-in-one-out.
            while (true)
            {
                if (priorSource == null)
                {
                    nextSource.Dispose();

                    throw new ObjectDisposedException(nameof(CancellationSeries));
                }

                CancellationTokenSource snapshot = Interlocked.CompareExchange(ref _cts, nextSource, priorSource);

                if (ReferenceEquals(snapshot, priorSource))
                {
                    break;
                }

                priorSource = snapshot;
            }

            priorSource.Cancel();
            priorSource.Dispose();

            return nextToken;
        }

        /// <summary>Gets whether this object has been disposed.</summary>
        public bool IsDisposed => Volatile.Read(ref _cts) == null;

        /// <inheritdoc />
        public void Dispose()
        {
            CancellationTokenSource source = Interlocked.Exchange(ref _cts, null);

            if (source == null)
            {
                // Already disposed
                return;
            }

            source.Cancel();
            source.Dispose();
        }
    }
}
