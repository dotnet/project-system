// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    /// <summary>
    /// A thread-safe object pool, backed by <see cref="ImmutableHashSet{T}"/> and using interlocked
    /// operations for optimistic, lock-free concurrent access.
    /// </summary>
    /// <remarks>
    /// Objects added to this pool will never be released.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal sealed class InternPool<T> where T : class
    {
        private ImmutableHashSet<T> _set;

        public int Count => _set.Count;

        public InternPool(IEqualityComparer<T>? comparer = null)
        {
            _set = ImmutableHashSet.Create(comparer);
        }

        public T Intern(T value)
        {
            // Would be nice if this was on ImmutableInterlocked as
            // requested in https://github.com/dotnet/corefx/issues/33653

            ImmutableHashSet<T> priorCollection = Volatile.Read(ref _set);

            bool successful;
            do
            {
                if (priorCollection.TryGetValue(value, out T existingValue))
                {
                    // The value already exists in the set. Return it.
                    return existingValue;
                }

                ImmutableHashSet<T> updatedCollection = priorCollection.Add(value);
                ImmutableHashSet<T> interlockedResult = Interlocked.CompareExchange(ref _set, updatedCollection, priorCollection);
                successful = ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // We already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            // We won the race-condition and have updated the collection.
            // Return the value that is in the collection (as of the Interlocked operation).
            return value;
        }
    }
}
