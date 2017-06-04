// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides additional context to log messages when they are logged to the log.
    /// </summary>
    internal struct ProjectLoggerContext
    {
        private readonly IProjectLogger _logger;
        private readonly FormatArray _formatArray;

        internal ProjectLoggerContext(IProjectLogger logger, FormatArray formatArray)
        {
            _logger = logger;
            _formatArray = formatArray;
        }

        /// <summary>
        ///     Writes the specified text, followed by the current line terminator, 
        ///     to the log.
        /// </summary>
        public void WriteLine(string text)
        {
            if (_logger.IsEnabled)
            {
                _logger.WriteLine(_formatArray.Text + " " + text);
            }
        }

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
        public void WriteLine(string format, object argument)
        {
            if (_logger.IsEnabled)
            {
                _logger.WriteLine(_formatArray.Text + " " + format, argument);
            }
        }

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
        public void WriteLine(string format, object argument1, object argument2)
        {
            if (_logger.IsEnabled)
            {
                _logger.WriteLine(_formatArray.Text + " " + format, argument1, argument2);
            }
        }

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
        public void WriteLine(string format, object argument1, object argument2, object argument3)
        {
            if (_logger.IsEnabled)
            {
                _logger.WriteLine(_formatArray.Text + " " + format, argument1, argument2, argument3);
            }
        }

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
        public void WriteLine(string format, params object[] arguments)
        {
            if (_logger.IsEnabled)
            {
                _logger.WriteLine(_formatArray.Text + " " + format, arguments);
            }
        }
    }
}
