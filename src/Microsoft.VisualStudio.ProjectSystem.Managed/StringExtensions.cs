// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio
{
    internal static class StringExtensions
    {
        public static string[] SplitReturningEmptyIfEmpty(this string value, params char[] separator)
        {
            string[] values = value.Split(separator);

            if (values.Length == 1 && string.IsNullOrEmpty(values[0]))
                return Array.Empty<string>();

            return values;
        }
    }
}
