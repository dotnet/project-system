// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio
{
    internal static class Strings
    {
        /// <inheritdoc cref="string.IsNullOrEmpty(string)"/>
        public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] string? s) => string.IsNullOrEmpty(s);

        /// <inheritdoc cref="string.IsNullOrWhiteSpace(string)"/>
        public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] string? s) => string.IsNullOrWhiteSpace(s);
    }
}
