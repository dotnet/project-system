// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides extension methods for <see cref="IProjectLogger"/> instances.
    /// </summary>
    internal static partial class ProjectLoggerExtensions
    {
        /// <summary>
        ///     Begins a logging batch that batches up logging writes
        ///     and writes them all at once on <see cref="IDisposable.Dispose"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        /// </exception>
        public static IProjectLoggerBatch BeginBatch(this IProjectLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));

            return new ProjectLoggerBatch(logger);
        }

        /// <summary>
        ///     If <see cref="IProjectLogger.IsEnabled"/> is <see langword="true"/>,
        ///     writes the current line terminator to the log.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        /// </exception>
        public static void WriteLine(this IProjectLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));

            logger.WriteLine(new StringFormat(string.Empty));
        }

        /// <summary>
        ///     If <see cref="IProjectLogger.IsEnabled"/> is <see langword="true"/>,
        ///     writes the specified text, followed by the current line terminator,
        ///     to the log.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        /// </exception>
        public static void WriteLine(this IProjectLogger logger, string text)
        {
            Requires.NotNull(logger, nameof(logger));

            logger.WriteLine(new StringFormat(text));
        }

        /// <summary>
        ///     If <see cref="IProjectLogger.IsEnabled"/> is <see langword="true"/>,
        ///     writes the text representation of the specified object, followed 
        ///     by the current line terminator, to the log using the specified 
        ///     format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        public static void WriteLine(this IProjectLogger logger, string format, object? argument)
        {
            Requires.NotNull(logger, nameof(logger));

            logger.WriteLine(new StringFormat(format, argument));
        }

        /// <summary>
        ///     If <see cref="IProjectLogger.IsEnabled"/> is <see langword="true"/>,
        ///     writes the text representation of the specified objects, followed 
        ///     by the current line terminator, to the log using the specified format
        ///     information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        public static void WriteLine(this IProjectLogger logger, string format, object? argument1, object? argument2)
        {
            Requires.NotNull(logger, nameof(logger));

            logger.WriteLine(new StringFormat(format, argument1, argument2));
        }

        /// <summary>
        ///     If <see cref="IProjectLogger.IsEnabled"/> is <see langword="true"/>,
        ///     writes the text representation of the specified objects, followed 
        ///     by the current line terminator, to the log using the specified 
        ///     format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        public static void WriteLine(this IProjectLogger logger, string format, object? argument1, object? argument2, object? argument3)
        {
            Requires.NotNull(logger, nameof(logger));

            logger.WriteLine(new StringFormat(format, argument1, argument2, argument3));
        }

        /// <summary>
        ///     If <see cref="IProjectLogger.IsEnabled"/> is <see langword="true"/>,
        ///     writes the  text representation of the specified array of objects, 
        ///     followed  by the current line terminator, to the log using the 
        ///     specified format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        public static void WriteLine(this IProjectLogger logger, string format, params object?[] arguments)
        {
            Requires.NotNull(logger, nameof(logger));

            logger.WriteLine(new StringFormat(format, arguments));
        }
    }
}
