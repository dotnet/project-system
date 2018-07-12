// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Provides an object that allows a common way to format a set of objects, only allocating if needed.
    /// </summary>
    internal struct StringFormat
    {
        // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
        // struct down to just 4 fields. These are only used for their "Length" property,
        // that is, their elements are never set or referenced.
        private static readonly object[] s_zeroArgumentArray = Array.Empty<object>();
        private static readonly object[] s_oneArgumentArray = new object[1];
        private static readonly object[] s_twoArgumentArray = new object[2];
        private static readonly object[] s_threeArgumentArray = new object[3];
        private readonly string _format;
        private readonly object _argument1;
        private readonly object _argument2;
        private readonly object _argument3;
        private readonly object[] _arguments;

        public StringFormat(string text)
        {
            _format = text;
            _argument1 = null;
            _argument2 = null;
            _argument3 = null;
            _arguments = s_zeroArgumentArray;
        }

        public StringFormat(string format, object argument)
        {
            _format = format;
            _argument1 = argument;
            _argument2 = null;
            _argument3 = null;
            _arguments = s_oneArgumentArray;
        }

        public StringFormat(string format, object argument1, object argument2)
        {
            _format = format;
            _argument1 = argument1;
            _argument2 = argument2;
            _argument3 = null;
            _arguments = s_twoArgumentArray;
        }

        public StringFormat(string format, object argument1, object argument2, object argument3)
        {
            _format = format;
            _argument1 = argument1;
            _argument2 = argument2;
            _argument3 = argument3;
            _arguments = s_threeArgumentArray;
        }

        public StringFormat(string format, object[] arguments)
        {
            _format = format;
            _argument1 = null;
            _argument2 = null;
            _argument3 = null;
            _arguments = arguments;
        }

        public object Argument1
        {
            get { return _argument1; }
        }

        public object Argument2
        {
            get { return _argument2; }
        }

        public object Argument3
        {
            get { return _argument3; }
        }

        public object[] Arguments
        {
            get { return _arguments; }
        }

        public string Format
        {
            get { return _format; }
        }

        public int Length
        {
            get { return _arguments.Length; }
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
