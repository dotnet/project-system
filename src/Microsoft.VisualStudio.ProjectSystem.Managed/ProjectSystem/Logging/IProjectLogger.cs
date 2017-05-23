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
        ///     Gets a value indicating if the logger is enabled.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if the <see cref="IProjectLogger"/> is enabled and logging to the log; otherwise, <see langword="false"/>.
        /// </value>
        bool IsEnabled
        {
            get;
        }

        /// <summary>
        ///     Writes the specified text, followed by the current line terminator, 
        ///     to the log.
        /// </summary>
        void WriteLine(string text);

        /// <summary>
        ///     Writes the text representation of the specified object, 
        ///     followed by the current line terminator, to the log using the 
        ///     specified format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        void WriteLine(string format, object argument);

        /// <summary>
        ///     Writes the text representation of the specified objects, 
        ///     followed by the current line terminator, to the log using the 
        ///     specified format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        void WriteLine(string format, object argument1, object argument2);

        /// <summary>
        ///     Writes the text representation of the specified objects, 
        ///     followed by the current line terminator, to the log using the 
        ///     specified format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        void WriteLine(string format, object argument1, object argument2, object argument3);

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