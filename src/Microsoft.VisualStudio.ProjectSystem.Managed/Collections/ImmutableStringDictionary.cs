// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace System.Collections.Immutable
{
    internal static class ImmutableStringDictionary<TValue>
    {
        public static readonly ImmutableDictionary<string, TValue> EmptyOrdinal
            = ImmutableDictionary<string, TValue>.Empty;

        public static readonly ImmutableDictionary<string, TValue> EmptyOrdinalIgnoreCase
            = ImmutableDictionary<string, TValue>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
    }
}
