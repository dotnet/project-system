// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Collections
{
    internal static class EnumerableExtensions
    {
        //
        // Summary:
        //     Creates a System.Collections.Generic.Dictionary`2 from an System.Collections.Generic.IEnumerable`1
        //     according to a specified key selector function, a comparer, and an element selector
        //     function. Unlike Enumerable.ToDictionary, this implementation allows callers to specify whether
        //     duplicate keys produced by the keySelector should not result in an ArgumentException
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable`1 to create a System.Collections.Generic.Dictionary`2
        //     from.
        //
        //   keySelector:
        //     A function to extract a key from each element.
        //
        //   elementSelector:
        //     A transform function to produce a result element value from each element.
        //
        //   comparer:
        //     An System.Collections.Generic.IEqualityComparer`1 to compare keys.
        //
        //   ignoreDuplicateKeys:
        //     A flag indicating whether an ArgumentException should be thrown if the keySelector produces duplicate keys.
        //
        // Type parameters:
        //   TSource:
        //     The type of the elements of source.
        //
        //   TKey:
        //     The type of the key returned by keySelector.
        //
        //   TElement:
        //     The type of the value returned by elementSelector.
        //
        // Returns:
        //     A System.Collections.Generic.Dictionary`2 that contains values of type TElement
        //     selected from the input sequence.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     source or keySelector or elementSelector is null.-or- keySelector produces a
        //     key that is null.
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, bool ignoreDuplicateKeys)
        {
            if (!ignoreDuplicateKeys)
                return source.ToDictionary(keySelector, elementSelector, comparer);

            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));

            Dictionary<TKey, TElement> result = new(comparer);

            foreach (var item in source)
            {
                var key = keySelector(item);

                if (key is null)
                    throw new ArgumentNullException(nameof(key));

                var element = elementSelector(item);
                result[key] = element;
            }

            return result;
        }
    }
}
