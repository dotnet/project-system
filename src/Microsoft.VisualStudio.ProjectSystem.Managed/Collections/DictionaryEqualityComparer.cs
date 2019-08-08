// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    {
        /// <summary>
        /// Backing field for the <see cref="Instance"/> static property.
        /// </summary>
        private static readonly DictionaryEqualityComparer<TKey, TValue> s_defaultInstance = new DictionaryEqualityComparer<TKey, TValue>();

        /// <summary>
        /// Initializes a new instance of the DictionaryEqualityComparer class.
        /// </summary>
        private DictionaryEqualityComparer()
        {
        }

        /// <summary>
        /// Gets a dictionary equality comparer instance appropriate for dictionaries that use the default key comparer for the <typeparamref name="TKey"/> type.
        /// </summary>
        internal static IEqualityComparer<IImmutableDictionary<TKey, TValue>> Instance
        {
            get { return s_defaultInstance; }
        }

        /// <summary>
        /// Checks two dictionaries for equality.
        /// </summary>
        public bool Equals(IImmutableDictionary<TKey, TValue> x, IImmutableDictionary<TKey, TValue> y)
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
                foreach (KeyValuePair<TKey, TValue> pair in obj)
                {
                    hashCode += keyComparer.GetHashCode(pair.Key) + valueComparer.GetHashCode(pair.Value);
                }
            }

            return hashCode;
        }
        /// <summary>
        /// Tests two dictionaries to see if their contents are identical.
        /// </summary>
        private static bool AreEquivalent(IImmutableDictionary<TKey, TValue> dictionary1, IImmutableDictionary<TKey, TValue> dictionary2)
        {
            Requires.NotNull(dictionary1, "dictionary1");

            if (dictionary1 == dictionary2)
            {
                return true;
            }

            IEqualityComparer<TValue> valueComparer = dictionary1 is ImmutableDictionary<TKey, TValue> concreteDictionary1 ? concreteDictionary1.ValueComparer : EqualityComparer<TValue>.Default;
            return AreEquivalent(dictionary1, dictionary2, valueComparer);
        }

        /// <summary>
        /// Tests two dictionaries to see if their contents are identical.
        /// </summary>
        private static bool AreEquivalent(IReadOnlyDictionary<TKey, TValue>? dictionary1, IReadOnlyDictionary<TKey, TValue>? dictionary2, IEqualityComparer<TValue> valueComparer)
        {
            Requires.NotNull(valueComparer, "valueComparer");

            if (dictionary1 == dictionary2)
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

            foreach (KeyValuePair<TKey, TValue> pair in dictionary1)
            {
                if (!dictionary2.TryGetValue(pair.Key, out TValue value) || !valueComparer.Equals(value, pair.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
