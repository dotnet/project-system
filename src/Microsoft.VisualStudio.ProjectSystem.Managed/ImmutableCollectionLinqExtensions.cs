// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio
{
    internal static class ImmutableCollectionLinqExtensions
    {
        // Most (all?) immutable collections provide non-allocating enumerators.
        //
        // This class provides replacements for common linq-like extension methods
        // that don't box to IEnumerable<T> and can therefore avoid allocation.

        public static int Count<TKey, TValue>(this ImmutableDictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
            where TKey : notnull
        {
            int count = 0;

            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                if (predicate(pair))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool Any<TKey, TValue>(this ImmutableDictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
            where TKey : notnull
        {
            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                if (predicate(pair))
                    return true;
            }

            return false;
        }

        public static T? FirstOrDefault<T, TArg>(this ImmutableArray<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (T obj in source)
            {
                if (predicate(obj, arg))
                    return obj;
            }

            return default;
        }
    }
}
