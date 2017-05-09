// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides an object that allows a common way to format a set of objects, only allocating if needed.
    /// </summary>
    internal struct FormatArray
    {
        // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
        // struct down to just 4 fields. These are only used for their "Length" property,
        // that is, their elements are never set or referenced.
        private static readonly object[] ZeroArgumentArray = Array.Empty<object>();
        private static readonly object[] OneArgumentArray = new object[1];
        private static readonly object[] TwoArgumentArray = new object[2];
        private static readonly object[] ThreeArgumentArray = new object[3];
        private readonly string _format;
        private readonly object _argument1;
        private readonly object _argument2;
        private readonly object _argument3;
        private readonly object[] _arguments;

        public FormatArray(string text)
        {
            _format = text;
            _argument1 = null;
            _argument2 = null;
            _argument3 = null;
            _arguments = ZeroArgumentArray;
        }

        public FormatArray(string format, object argument)
        {
            _format = format;
            _argument1 = argument;
            _argument2 = null;
            _argument3 = null;
            _arguments = OneArgumentArray;
        }

        public FormatArray(string format, object argument1, object argument2)
        {
            _format = format;
            _argument1 = argument1;
            _argument2 = argument2;
            _argument3 = null;
            _arguments = TwoArgumentArray;
        }

        public FormatArray(string format, object argument1, object argument2, object argument3)
        {
            _format = format;
            _argument1 = argument1;
            _argument2 = argument2;
            _argument3 = argument3;
            _arguments = ThreeArgumentArray;
        }

        public FormatArray(string format, object[] arguments)
        {
            _format = format;
            _argument1 = null;
            _argument2 = null;
            _argument3 = null;
            _arguments = arguments;
        }

        public string Text
        {
            get
            {
                int length = _arguments.Length;

                // Making sure we call through the non-params array version of String.Format 
                // where possible to avoid "params" array allocation.
                if (length == 0)
                    return _format;

                if (length == 1)
                    return string.Format(CultureInfo.CurrentCulture, _format, _argument1);

                if (length == 2)
                    return string.Format(CultureInfo.CurrentCulture, _format, _argument1, _argument2);

                if (length == 3)
                    return string.Format(CultureInfo.CurrentCulture, _format, _argument1, _argument2, _argument3);

                return string.Format(CultureInfo.CurrentCulture, _format, _arguments);
            }
        }
    }
}
