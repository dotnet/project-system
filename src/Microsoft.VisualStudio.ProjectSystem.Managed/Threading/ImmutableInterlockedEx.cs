// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.VisualStudio.Threading
{
    /// <summary>
    /// Contains interlocked exchange mechanisms for immutable collections,
    /// complementary to those in <see cref="ImmutableInterlocked"/>.
    /// </summary>
    internal static class ImmutableInterlockedEx
    {
        // NOTE InterlockedImmutable.GetOrAdd exists for ImmutableDictionary<,> but not for sets

        /// <summary>
        /// Gets the value for the specified key from the dictionary, or if the key was not found, adds a new value to the dictionary.
        /// </summary>
        /// <param name="location">The variable or field to query and atomically update if the specified value is not in the set.</param>
        /// <param name="value">The value to add to the set if not already present.</param>
        /// <typeparam name="T">The type of the values contained in the collection.</typeparam>
        /// <returns>The existing value if found, otherwise <paramref name="value" />.</returns>
        public static T GetOrAdd<T>(ref ImmutableHashSet<T> location, T value)
        {
            ImmutableHashSet<T> prior = Volatile.Read(ref location);

            Requires.NotNull(prior, nameof(location)); // intentionally mismatched names here

            while (true)
            {
                if (prior.TryGetValue(value, out T existingValue))
                {
                    // The value already exists in the set. Return it.
                    return existingValue;
                }

                // Add a value to the set. This will succeed as we've just seen that value is not in prior.
                ImmutableHashSet<T> updated = prior.Add(value);

                // Attempt to update the field optimistically.
                ImmutableHashSet<T> original = Interlocked.CompareExchange(ref location, updated, prior);

                if (ReferenceEquals(original, prior))
                {
                    // The update was successful; return the added icon set.
                    return value;
                }

                // Optimistic update failed, so loop around and try again.
                prior = original;
            }
        }
    }
}
