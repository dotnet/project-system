// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Microsoft.VisualStudio
{
    internal static class LinqExtensions
    {
        [Pure]
        [return: MaybeNull]
        public static T FirstOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (T obj in source)
            {
                if (predicate(obj, arg))
                    return obj;
            }

            return default!;
        }

        [Pure]
        [return: MaybeNull]
        public static T SingleOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            using (IEnumerator<T> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    T match = enumerator.Current;

                    if (predicate(match, arg))
                    {
                        // Check all remaining items to ensure there is only a single match
                        while (enumerator.MoveNext())
                        {
                            if (predicate(enumerator.Current, arg))
                            {
                                throw new InvalidOperationException("More than one element matches predicate.");
                            }
                        }

                        return match;
                    }
                }
            }

            return default!;
        }

        /// <summary>
        ///     Returns distinct elements from a sequence by using a specified key selector and <see cref="IEqualityComparer{T}"/> to compare values.
        /// </summary>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return DistinctIterator(source, keySelector, comparer);
        }

        private static IEnumerable<TSource> DistinctIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            HashSet<TKey>? set = null;
            foreach (TSource element in source)
            {
                // Avoid allocating unless needed
                set ??= new HashSet<TKey>(comparer);

                TKey key = keySelector(element);

                if (set.Add(key))
                {
                    yield return element;
                }
            }
        }
    }
}
