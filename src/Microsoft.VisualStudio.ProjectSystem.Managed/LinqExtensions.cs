// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Contracts;

namespace Microsoft.VisualStudio
{
    internal static class LinqExtensions
    {
        [Pure]
        public static T? FirstOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (T obj in source)
            {
                if (predicate(obj, arg))
                    return obj;
            }

            return default;
        }

        [Pure]
        public static T? SingleOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            using IEnumerator<T> enumerator = source.GetEnumerator();

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

            return default;
        }

        [Pure]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            foreach (T? item in source)
            {
                if (item is not null)
                    yield return item;
            }
        }

        /// <summary>
        /// Specialisation of <see cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        /// that avoids allocation when the sequence is statically known to be an array.
        /// </summary>
        public static bool Any<T>(this T[] array, Func<T, bool> predicate)
        {
            foreach (T item in array)
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Specialisation of <see cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        /// that avoids allocation when the sequence is statically known to be an array.
        /// </summary>
        public static bool Any<T, TArg>(this T[] array, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (T item in array)
            {
                if (predicate(item, arg))
                {
                    return true;
                }
            }

            return false;
        }

        public static ImmutableArray<TValue> ToImmutableValueArray<TKey, TValue>(this Dictionary<TKey, TValue> source)
        {
            ImmutableArray<TValue>.Builder builder = ImmutableArray.CreateBuilder<TValue>(source.Count);

            foreach ((_, TValue value) in source)
            {
                builder.Add(value);
            }

            return builder.MoveToImmutable();
        }

        public static TOut[] SelectArray<TIn, TOut>(this TIn[] array, Func<TIn, TOut> selector)
        {
            TOut[] output = new TOut[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                output[i] = selector(array[i]);
            }

            return output;
        }
    }
}
