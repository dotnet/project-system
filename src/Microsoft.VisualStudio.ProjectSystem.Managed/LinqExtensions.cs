// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Microsoft.VisualStudio
{
    internal static class LinqExtensions
    {
        [Pure]
        public static T FirstOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (T obj in source)
            {
                if (predicate(obj, arg))
                    return obj;
            }

            return default;
        }

        [Pure]
        public static T SingleOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            T match = default;
            long count = 0;

            foreach (T item in source)
            {
                if (predicate(item, arg))
                {
                    match = item;
                    checked { ++count; }
                }
            }

            if (count == 0)
                return default;
            if (count == 1)
                return match;
            throw new InvalidOperationException("More than one element matches predicate.");
        }
    }
}
