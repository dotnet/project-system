// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides methods for logging project messages.
    /// </summary>
    internal interface IProjectLogger
    {
        /// <summary>
        ///     Writes the specified text, followed by the current line terminator, 
        ///     to the log.
        /// </summary>
        void WriteLine(string text);


        /// <summary>
        ///     Writes the text representation of the specified array of objects, 
        ///     followed by the current line terminator, to the log using the 
        ///     specified format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        void WriteLine(string format, params object[] arguments);
    }
}