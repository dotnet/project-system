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

        public static ImmutableArray<TOutput> SelectImmutableArray<TInput, TOutput>(this ImmutableArray<TInput> values, Func<TInput, TOutput> selector)
        {
            if (values.IsDefaultOrEmpty)
                return [];

            ImmutableArray<TOutput>.Builder builder = ImmutableArray.CreateBuilder<TOutput>(initialCapacity: values.Length);

            foreach (TInput value in values)
            {
                builder.Add(selector(value));
            }

            return builder.MoveToImmutable();
        }

        public static ImmutableArray<TOutput> ToImmutableArray<TOutput, TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary, Func<TKey, TValue, TOutput> factory)
        {
            if (dictionary.Count == 0)
            {
                return ImmutableArray<TOutput>.Empty;
            }

            ImmutableArray<TOutput>.Builder builder = ImmutableArray.CreateBuilder<TOutput>(initialCapacity: dictionary.Count);

            foreach ((TKey key, TValue value) in dictionary)
            {
                builder.Add(factory(key, value));
            }

            return builder.MoveToImmutable();
        }
    }
}
