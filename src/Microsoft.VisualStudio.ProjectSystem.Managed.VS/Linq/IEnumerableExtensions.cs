// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Linq;

internal static class IEnumerableExtensions
{
    /// <summary>
    /// Returns the element that has the maximum value according to the specified key selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum element of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>The value with the maximum key in the sequence.</returns>
    public static TSource MaxBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
        where TKey : IComparable<TKey>
    {
        return MaxByOrDefault(source, keySelector, throwIfEmpty: true)!;
    }

    /// <summary>
    /// Returns the element that has the maximum value according to the specified key selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum element of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>The value with the maximum key in the sequence.</returns>
    public static TSource? MaxByOrDefault<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
        where TKey : IComparable<TKey>
    {
        return MaxByOrDefault(source, keySelector, throwIfEmpty: false);
    }

    /// <summary>
    /// Returns the element that has the maximum value according to the specified key selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum element of.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="throwIfEmpty">Indicates that method should throw if an empty IEnumerable is passed.</param>
    /// <returns>The value with the maximum key in the sequence.</returns>
    private static TSource? MaxByOrDefault<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        bool throwIfEmpty)
        where TKey : IComparable<TKey>
    {
        using (IEnumerator<TSource> enumerator = source.GetEnumerator())
        {
            if (!enumerator.MoveNext())
            {
                if (throwIfEmpty)
                {
                    throw new InvalidOperationException("Sequence contains no elements.");
                }
                return default;
            }

            TSource maxElement = enumerator.Current;
            TKey maxKey = keySelector(maxElement);

            while (enumerator.MoveNext())
            {
                TSource candidate = enumerator.Current;
                TKey candidateKey = keySelector(candidate);
                if (candidateKey.CompareTo(maxKey) > 0)
                {
                    maxKey = candidateKey;
                    maxElement = candidate;
                }
            }

            return maxElement;
        }
    }
}
