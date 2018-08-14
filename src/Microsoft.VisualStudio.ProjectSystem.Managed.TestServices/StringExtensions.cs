// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
