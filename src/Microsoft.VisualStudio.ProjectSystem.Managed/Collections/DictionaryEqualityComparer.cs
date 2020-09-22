// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Collections
{
    /// <summary>
    /// Provides simple dictionary equality checks.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the dictionaries to compare.</typeparam>
    /// <typeparam name="TValue">The type of value in the dictionaries to compare.</typeparam>
    internal class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<IImmutableDictionary<TKey, TValue>>
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the DictionaryEqualityComparer class.
        /// </summary>
        private DictionaryEqualityComparer()
        {
        }

        /// <summary>
        /// Gets a dictionary equality comparer instance appropriate for dictionaries that use the default key comparer for the <typeparamref name="TKey"/> type.
        /// </summary>
        internal static DictionaryEqualityComparer<TKey, TValue> Instance { get; } = new DictionaryEqualityComparer<TKey, TValue>();

        /// <summary>
        /// Checks two dictionaries for equality.
        /// </summary>
        public bool Equals(IImmutableDictionary<TKey, TValue>? x, IImmutableDictionary<TKey, TValue>? y)
        {
            return AreEquivalent(x, y);
        }

        /// <summary>
        /// Calculates a hash code for a dictionary.
        /// </summary>
        public int GetHashCode(IImmutableDictionary<TKey, TValue> obj)
        {
            int hashCode = 0;

            var concreteDictionary1 = obj as ImmutableDictionary<TKey, TValue>;
            IEqualityComparer<TKey> keyComparer = concreteDictionary1 != null ? concreteDictionary1.KeyComparer : EqualityComparer<TKey>.Default;
            IEqualityComparer<TValue> valueComparer = concreteDictionary1 != null ? concreteDictionary1.ValueComparer : EqualityComparer<TValue>.Default;

            if (obj != null)
            {
                foreach ((TKey key, TValue value) in obj)
                {
                    hashCode += keyComparer.GetHashCode(key) ^ valueComparer.GetHashCode(value);
                }
            }

            return hashCode;
        }
        /// <summary>
        /// Tests two dictionaries to see if their contents are identical.
        /// </summary>
        private static bool AreEquivalent(IImmutableDictionary<TKey, TValue>? dictionary1, IImmutableDictionary<TKey, TValue>? dictionary2)
        {
            if (ReferenceEquals(dictionary1, dictionary2))
            {
                return true;
            }

            if (dictionary1 == null || dictionary2 == null)
            {
                return false;
            }

            IEqualityComparer<TValue> valueComparer = dictionary1 is ImmutableDictionary<TKey, TValue> concreteDictionary1 ? concreteDictionary1.ValueComparer : EqualityComparer<TValue>.Default;
            return AreEquivalent(dictionary1, dictionary2, valueComparer);
        }

        /// <summary>
        /// Tests two dictionaries to see if their contents are identical.
        /// </summary>
        private static bool AreEquivalent(IReadOnlyDictionary<TKey, TValue>? dictionary1, IReadOnlyDictionary<TKey, TValue>? dictionary2, IEqualityComparer<TValue> valueComparer)
        {
            Requires.NotNull(valueComparer, nameof(valueComparer));

            if (ReferenceEquals(dictionary1, dictionary2))
            {
                return true;
            }

            if (dictionary1 == null || dictionary2 == null)
            {
                return false;
            }

            if (dictionary1.Count != dictionary2.Count)
            {
                return false;
            }

            if (dictionary1.Count == 0)
            {
                // both dictionaries are empty, so bail out early to avoid
                // allocating an IEnumerator.
                return true;
            }

            foreach ((TKey key, TValue value1) in dictionary1)
            {
                if (!dictionary2.TryGetValue(key, out TValue value2) || !valueComparer.Equals(value1, value2))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
