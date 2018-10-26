// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Contains commonly used <see cref="IEqualityComparer{String}"/> instances.
    /// </summary>
    /// <remarks>
    ///     Mirrors values in <see cref="StringComparisons"/>.
    /// </remarks>
    internal static class StringComparers
    {
        public static IEqualityComparer<string> WorkspaceProjectContextIds => StringComparer.Ordinal;
        public static IEqualityComparer<string> Paths => StringComparer.OrdinalIgnoreCase;
        public static IEqualityComparer<string> PropertyNames => StringComparer.OrdinalIgnoreCase;
        public static IEqualityComparer<string> PropertyValues => StringComparer.OrdinalIgnoreCase;
        public static IEqualityComparer<string> RuleNames => StringComparer.OrdinalIgnoreCase;
        public static IEqualityComparer<string> ConfigurationDimensionNames => StringComparer.Ordinal;
        public static IEqualityComparer<string> DependencyProviderTypes => StringComparer.OrdinalIgnoreCase;
        public static IEqualityComparer<string> ItemTypes => StringComparer.OrdinalIgnoreCase;
    }

    /// <summary>
    ///     Contains commonly used <see cref="StringComparison"/> instances.
    /// </summary>
    /// <remarks>
    ///     Mirrors values in <see cref="StringComparers"/>.
    /// </remarks>
    internal static class StringComparisons
    {
        public static StringComparison WorkspaceProjectContextIds => StringComparison.Ordinal;
        public static StringComparison Paths => StringComparison.OrdinalIgnoreCase;
        public static StringComparison PropertyNames => StringComparison.OrdinalIgnoreCase;
        public static StringComparison PropertyValues => StringComparison.OrdinalIgnoreCase;
        public static StringComparison RuleNames => StringComparison.OrdinalIgnoreCase;
        public static StringComparison ConfigurationDimensionNames => StringComparison.Ordinal;
        public static StringComparison DependencyProviderTypes => StringComparison.OrdinalIgnoreCase;
        public static StringComparison ItemTypes => StringComparison.OrdinalIgnoreCase;
    }
}
