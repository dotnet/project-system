// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an implementation of <see cref="OnceInitializedOnceDisposedAsync"/> that lets
    ///     implementers protect themselves from being disposed while doing work.
    /// </summary>
    /// <remarks>
    ///     <see cref="OnceInitializedOnceDisposed"/> lets implementors prevent themselves from being disposed
    ///     by locking <see cref="OnceInitializedOnceDisposed.SyncObject"/>. This class provides a similar
    ///     mechanism by passing a delegate into <see cref="ExecuteUnderLockAsync"/>.
    /// </remarks>
    internal abstract class OnceInitializedOnceDisposedUnderLockAsync : OnceInitializedOnceDisposedAsync
    {
        private readonly ReentrantSemaphore _semaphore;

        protected OnceInitializedOnceDisposedUnderLockAsync(JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
            _semaphore = ReentrantSemaphore.Create(1, joinableTaskContextNode.Context, ReentrantSemaphore.ReentrancyMode.Stack);
        }

        protected sealed override async Task DisposeCoreAsync(bool initialized)
        {
            await _semaphore.ExecuteAsync(() => DisposeCoreUnderLockAsync(initialized));

            _semaphore.Dispose();
        }

        /// <summary>
        ///     Disposes of managed and unmanaged resources owned by this instance, under a lock
        ///     that prevents overlap with any currently executing actions passed to
        ///     <see cref="ExecuteUnderLockAsync(Func{CancellationToken, Task}, CancellationToken)"/>.
        /// </summary>
        /// <param name="initialized">
        ///     A value indicating whether this instance had been previously initialized.
        /// </param>
        protected abstract Task DisposeCoreUnderLockAsync(bool initialized);

        /// <summary>
        ///     Executes the specified action under a lock that prevents overlap with any currently executing actions passed to
        ///     <see cref="ExecuteUnderLockAsync(Func{CancellationToken, Task}, CancellationToken)"/> and
        ///     <see cref="OnceInitializedOnceDisposedAsync.DisposeAsync"/>.
        /// </summary>
        /// <param name="action">
        ///     The action to execute under the lock. This action is passed a <see cref="CancellationToken"/> which is cancelled
        ///     when either this object is disposed, or <paramref name="cancellationToken"/> is cancelled.
        /// </param>
        /// <param name="cancellationToken">
        ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        ///     The result of <paramref name="action"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and <paramref name="cancellationToken"/> is cancelled.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The result is awaited and the <see cref="OnceInitializedOnceDisposedUnderLockAsync"/>
        ///     has been disposed of.
        /// </exception>
        protected Task<T> ExecuteUnderLockAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(action, nameof(action));

            return ExecuteUnderLockCoreAsync(action, cancellationToken);
        }

        /// <summary>
        ///     Executes the specified action under a lock that prevents overlap with any currently executing actions passed to
        ///     <see cref="ExecuteUnderLockAsync(Func{CancellationToken, Task}, CancellationToken)"/> and
        ///     <see cref="OnceInitializedOnceDisposedAsync.DisposeAsync"/>.
        /// </summary>
        /// <param name="action">
        ///     The action to execute under the lock. This action is passed a <see cref="CancellationToken"/> which is cancelled
        ///     when either this object is disposed, or <paramref name="cancellationToken"/> is cancelled.
        /// </param>
        /// <param name="cancellationToken">
        ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        ///     The result of <paramref name="action"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and <paramref name="cancellationToken"/> is cancelled.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The result is awaited and the <see cref="OnceInitializedOnceDisposedUnderLockAsync"/>
        ///     has been disposed of.
        /// </exception>
        protected Task ExecuteUnderLockAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(action, nameof(action));

            return ExecuteUnderLockCoreAsync(action, cancellationToken);
        }

        private async Task<T> ExecuteUnderLockCoreAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(action, nameof(action));

            using var source = CancellationTokenExtensions.CombineWith(cancellationToken, DisposalToken);
            CancellationToken jointCancellationToken = source.Token;

            try
            {
                T result = default!;
                await _semaphore.ExecuteAsync(async () => { result = await action(jointCancellationToken); }, jointCancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                if (!TryTranslateException(ex, cancellationToken, DisposalToken))
                    throw;
            }

            throw Assumes.NotReachable();
        }

        private async Task ExecuteUnderLockCoreAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(action, nameof(action));

            using var source = CancellationTokenExtensions.CombineWith(cancellationToken, DisposalToken);
            CancellationToken jointCancellationToken = source.Token;

            try
            {
                await _semaphore.ExecuteAsync(() => action(jointCancellationToken), jointCancellationToken);
            }
            catch (Exception ex)
            {
                if (!TryTranslateException(ex, cancellationToken, DisposalToken))
                    throw;
            }
        }

        private bool TryTranslateException(Exception exception, CancellationToken cancellationToken1, CancellationToken cancellationToken2)
        {
            // Consumers treat cancellation due their specified token being cancelled differently from cancellation 
            // due our instance being disposed. Because we wrap the two in a source, the resulting exception
            // contains it instead of the one that was actually cancelled.
            if (exception is OperationCanceledException)
            {
                cancellationToken1.ThrowIfCancellationRequested();
                cancellationToken2.ThrowIfCancellationRequested();
            }

            if (exception is ObjectDisposedException && DisposalToken.IsCancellationRequested)
            {
                // There's a tiny chance that between checking the cancellation token (wrapping DisposalToken) 
                // and checking if the underlying SemaphoreSlim has been disposed, that dispose for this instance 
                // (and hence _semaphore) has been run. Handle that and just treat it as a cancellation.
                DisposalToken.ThrowIfCancellationRequested();
            }

            return false;
        }
    }
}
