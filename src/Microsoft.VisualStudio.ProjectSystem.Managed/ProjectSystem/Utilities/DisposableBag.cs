// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information. 

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// A class that tracks a set of disposable objects and a cancellation token for purposes
    /// of managing the lifetime of a version-sync'd block join.
    /// </summary>
    internal class DisposableBag : IDisposableObservable
    {
        /// <summary>
        /// The source of the cancellation token exposed to the join.
        /// </summary>
        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// The registration that automatically disposes this object when and if the cancellation token is ever canceled.
        /// </summary>
        private readonly CancellationTokenRegistration _autoDisposeRegistration;

        /// <summary>
        /// A token based on the <see cref="_cts"/>, so that it's accessible even after disposal.
        /// </summary>
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// The set of disposable blocks.
        /// </summary>
        private ImmutableHashSet<IDisposable> _disposables = ImmutableHashSet.Create<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableBag"/> class.
        /// </summary>
        internal DisposableBag(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationToken = _cts.Token;
            _autoDisposeRegistration = RegisterNoThrowOnDispose(cancellationToken, Dispose);
        }

        /// <summary>
        /// A value indicating whether this instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the cancellation token that signals the user has terminated the link.
        /// </summary>
        internal CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
        }

        /// <summary>
        /// Disposes of all contained links and signals the cancellation token.
        /// </summary>
        public void Dispose()
        {
            bool disposedThisTime = false;
            var disposables = ImmutableHashSet.Create<IDisposable>();
            lock (this)
            {
                if (!IsDisposed)
                {
                    // Two related cancellation tokens both end up tending to call this method, at roughly the same time.
                    // So to avoid deadlocks with those tokens themselves, it's imperative that we very carefully avoid
                    // executing outside (even framework) code within this lock.
                    disposedThisTime = true;
                    IsDisposed = true;
                    disposables = _disposables;
                    _disposables = _disposables.Clear();
                }
            }

            // Because cancelling a CancellationTokenSource can cause arbitrary code to run when cancellation occurs -
            // specifically the user can register a callback with CancellationToken.Register() - we cannot call Cancel
            // on the token source while holding a lock, as this can cause deadlocks.
            if (disposedThisTime)
            {
                _autoDisposeRegistration.Dispose();
                _cts.Cancel();
                _cts.Dispose();
                DisposeAllIfNotNull(disposables);
            }
        }

        /// <summary>
        /// Adds a value to be disposed of when this collection is disposed of or canceled.
        /// </summary>
        /// <param name="disposable">The value to be disposed of later. May be <c>null</c>.</param>
        internal void AddDisposable(IDisposable disposable)
        {
            if (disposable == null)
            {
                return;
            }

            bool shouldDisposeArgument = false;
            lock (this)
            {
                if (IsDisposed)
                {
                    shouldDisposeArgument = true;
                }
                else
                {
                    _disposables = _disposables.Add(disposable);
                }
            }

            if (shouldDisposeArgument)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Adds values to be disposed of when this collection is disposed of or canceled.
        /// </summary>
        internal void AddDisposables(IEnumerable<IDisposable> disposables)
        {
            Requires.NotNull(disposables, nameof(disposables));

            foreach (IDisposable disposable in disposables)
            {
                AddDisposable(disposable);
            }
        }

        /// <summary>
        /// Removes a disposable value from the collection.
        /// </summary>
        /// <param name="disposable">The value to remove. May be <c>null</c>.</param>
        internal void RemoveDisposable(IDisposable disposable)
        {
            if (disposable == null)
            {
                return;
            }

            lock (this)
            {
                _disposables = _disposables.Remove(disposable);
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when and if a token is canceled,
        /// protecting against <see cref="ObjectDisposedException"/> in the event that the
        /// <see cref="CancellationTokenSource"/> has already been disposed.
        /// </summary>
        internal static CancellationTokenRegistration RegisterNoThrowOnDispose(CancellationToken token, Action callback)
        {
            try
            {
                return token.Register(callback);
            }
            catch (ObjectDisposedException)
            {
                // The CancellationTokenSource has already been disposed.  It rejected the register.
                // But now we know the CancellationToken is in its final state (either cancelled or not).
                // So simulate the right behavior by invoking the callback or not, based on whether it was
                // already canceled.
                if (token.IsCancellationRequested)
                {
                    callback();
                }

                return new CancellationTokenRegistration();
            }
        }

        /// <summary>
        /// Calls <see cref="IDisposable.Dispose"/> on all elements in a sequence,
        /// allowing the sequence itself or elements inside it to be null.
        /// </summary>
        internal static void DisposeAllIfNotNull(IEnumerable<IDisposable> sequence, bool cacheSequence = false)
        {
            if (sequence != null)
            {
                if (cacheSequence)
                {
                    // This makes us impervious to changes in the sequence generated by disposing elements of the sequence.
                    sequence = sequence.ToList();
                }

                foreach (IDisposable item in sequence)
                {
                    if (item != null)
                    {
                        item.Dispose();
                    }
                }
            }
        }
    }
}
