// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Collections
{
    /// <summary>
    /// Compares <see cref="Dictionary{TKey, TValue}"/> instances for equality of keys and values.
    /// Also compares equality of item order.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the dictionaries to compare.</typeparam>
    /// <typeparam name="TValue">The type of value in the dictionaries to compare.</typeparam>
    internal sealed class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
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
        internal static DictionaryEqualityComparer<TKey, TValue> Instance { get; } = new();

        /// <summary>
        /// Checks two dictionaries for equality.
        /// </summary>
        public bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Count != y.Count)
            {
                return false;
            }

            if (x.Count == 0)
            {
                // both dictionaries are empty, so bail out early to avoid
                // enumerator allocation
                return true;
            }

            // NOTE this uses the comparer from x, not from y
            IEqualityComparer<TKey> keyComparer = x.Comparer ?? EqualityComparer<TKey>.Default;
            IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            // Compare items based on their order, as we care about preserving order in launch profiles
            using Dictionary<TKey, TValue>.Enumerator enumerator1 = x.GetEnumerator();
            using Dictionary<TKey, TValue>.Enumerator enumerator2 = y.GetEnumerator();

            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                (TKey key1, TValue value1) = enumerator1.Current;
                (TKey key2, TValue value2) = enumerator2.Current;

                if (!keyComparer.Equals(key1, key2) || !valueComparer.Equals(value1, value2))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculates a hash code for a dictionary.
        /// </summary>
        public int GetHashCode(Dictionary<TKey, TValue> obj)
        {
            int hashCode = 0;

            if (obj is not null)
            {
                IEqualityComparer<TKey> keyComparer = obj.Comparer ?? EqualityComparer<TKey>.Default;
                IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

                foreach ((TKey key, TValue value) in obj)
                {
                    hashCode += keyComparer.GetHashCode(key) ^ valueComparer.GetHashCode(value);
                }
            }

            return hashCode;
        }
    }
}
