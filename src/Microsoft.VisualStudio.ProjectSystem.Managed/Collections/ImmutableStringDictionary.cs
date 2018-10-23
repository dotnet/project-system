// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
