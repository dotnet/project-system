// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Contains commonly-used delimiters used for splitting <see cref="string"/> instances.
    /// </summary>
    internal static class Delimiter
    {
        /// <summary>
        /// Single, static instance of an array that contains a comma ',', which is used to split strings.
        /// </summary>
        internal static readonly char[] Comma = new char[] { ',' };

        /// <summary>
        /// Single, static instance of an array that contains a period '.', which is used to split strings.
        /// </summary>
        internal static readonly char[] Period = new char[] { '.' };

        /// <summary>
        /// Single, static instance of an array that contains a semi-colon ';', which is used to split strings.
        /// </summary>
        internal static readonly char[] Semicolon = new char[] { ';' };

        /// <summary>
        /// Single, static instance of an array that contains a forward slash '/', which is used to split strings.
        /// </summary>
        internal static readonly char[] ForwardSlash = new char[] { '/' };

        /// <summary>
        /// Single, static instance of an array that contains a back slash '\', which is used to split strings.
        /// </summary>
        internal static readonly char[] BackSlash = new char[] { '\\' };

        /// <summary>
        /// Single, static instance of an array that contains '+' and '-' characters.
        /// </summary>
        internal static readonly char[] PlusAndMinus = new char[] { '+', '-' };

        /// <summary>
        /// Single, static instance of an array that contains platform-specific path separators
        /// </summary>
        internal static readonly char[] Path = new char[]
        {
            System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar
        };
    }
}
