// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Contains commonly-used delimiters used for splitting <see cref="string"/> instances.
    /// </summary>
    internal static class Delimiter
    {
        /// <summary>
        /// Single, static instance of an array that contains a semi-colon ';', which is used to split strings, etc.
        /// </summary>
        internal static readonly char[] Semicolon = new char[] { ';' };

        /// <summary>
        /// Single, static instance of an array that contains a period '.', which is used to split strings, etc.
        /// </summary>
        internal static readonly char[] Period = new char[] { '.' };

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
