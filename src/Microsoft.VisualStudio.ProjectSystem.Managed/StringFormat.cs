// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Provides an object that allows a common way to format a set of objects, only allocating if needed.
    /// </summary>
    internal readonly struct StringFormat
    {
        // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
        // struct down to just 4 fields. These are only used for their "Length" property,
        // that is, their elements are never set or referenced.
        private static readonly object[] s_zeroArgumentArray = Array.Empty<object>();
        private static readonly object[] s_oneArgumentArray = new object[1];
        private static readonly object[] s_twoArgumentArray = new object[2];
        private static readonly object[] s_threeArgumentArray = new object[3];

        public StringFormat(string text)
        {
            Format = text;
            Argument1 = null;
            Argument2 = null;
            Argument3 = null;
            Arguments = s_zeroArgumentArray;
        }

        public StringFormat(string format, object? argument)
        {
            Format = format;
            Argument1 = argument;
            Argument2 = null;
            Argument3 = null;
            Arguments = s_oneArgumentArray;
        }

        public StringFormat(string format, object? argument1, object? argument2)
        {
            Format = format;
            Argument1 = argument1;
            Argument2 = argument2;
            Argument3 = null;
            Arguments = s_twoArgumentArray;
        }

        public StringFormat(string format, object? argument1, object? argument2, object? argument3)
        {
            Format = format;
            Argument1 = argument1;
            Argument2 = argument2;
            Argument3 = argument3;
            Arguments = s_threeArgumentArray;
        }

        public StringFormat(string format, object?[] arguments)
        {
            Requires.Range(arguments.Length > 3, nameof(arguments), "Must contain at least three items");
            Format = format;
            Argument1 = null;
            Argument2 = null;
            Argument3 = null;
            Arguments = arguments;
        }

        public object? Argument1 { get; }

        public object? Argument2 { get; }

        public object? Argument3 { get; }

        public object?[] Arguments { get; }

        public string Format { get; }

        public int Length => Arguments.Length;

        public string Text
        {
            get
            {
                int length = Arguments.Length;

                // Making sure we call through the non-params array version of String.Format 
                // where possible to avoid "params" array allocation.
                if (length == 0)
                    return Format;

                if (length == 1)
                    return string.Format(CultureInfo.CurrentCulture, Format, Argument1);

                if (length == 2)
                    return string.Format(CultureInfo.CurrentCulture, Format, Argument1, Argument2);

                if (length == 3)
                    return string.Format(CultureInfo.CurrentCulture, Format, Argument1, Argument2, Argument3);

                return string.Format(CultureInfo.CurrentCulture, Format, Arguments);
            }
        }
    }
}
