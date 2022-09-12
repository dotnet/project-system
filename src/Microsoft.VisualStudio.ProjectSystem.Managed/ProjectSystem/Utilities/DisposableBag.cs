// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Tracks a bag of distinct disposable objects which will be disposed when the bag itself is disposed.
    /// </summary>
    internal sealed class DisposableBag : IDisposable, IEnumerable
    {
        /// <summary>
        /// The set of disposable blocks. If <see langword="null" />, then this disposable bag has been disposed.
        /// </summary>
        private ImmutableHashSet<IDisposable>? _disposables = ImmutableHashSet.Create<IDisposable>();

        /// <summary>
        /// Disposes of all contained disposable items.
        /// </summary>
        public void Dispose()
        {
            ImmutableHashSet<IDisposable>? disposables = Interlocked.Exchange(ref _disposables, null);

            if (disposables is not null)
            {
                foreach (IDisposable? item in disposables)
                {
                    item?.Dispose();
                }
            }
        }

        /// <summary>
        /// Adds an object to this bag, to be disposed when the bag itself is disposed.
        /// </summary>
        /// <remarks>
        /// If this disposable bag has already been disposed, <paramref name="disposable" /> will be disposed immediately.
        /// </remarks>
        /// <param name="disposable">The value to be included in this disposable bag.</param>
        public void Add(IDisposable? disposable)
        {
            if (disposable is null)
            {
                return;
            }

            bool shouldDisposeArgument = false;
            ImmutableInterlocked.Update(
                ref _disposables,
                (set, item) =>
                {
                    if (set is null)
                    {
                        shouldDisposeArgument = true;
                        return null!;
                    }

                    return set.Add(item);
                },
                disposable);

            if (shouldDisposeArgument)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Implemented only to allow collection initialization of this type.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
    }
}
