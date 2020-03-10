// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

using System;

namespace Microsoft
{
    internal static class StringExtensions
    {
        public static string[] SplitReturningEmptyIfEmpty(this string value, char separator)
        {
            string[] values = value.Split(separator);

            if (values.Length == 1 && string.IsNullOrEmpty(value))
                return Array.Empty<string>();

            return values;
        }
    }
}
