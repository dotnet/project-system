// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio
{
    internal static class ImmutableCollectionLinqExtensions
    {
        // Most (all?) immutable collections provide non-allocating enumerators.
        //
        // This class provides replacements for common linq-like extension methods
        // that don't box to IEnumerable<T> and can therefore avoid allocation.

        public static int Count<TKey, TValue>(this ImmutableDictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
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
    }
}
