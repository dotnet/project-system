// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides extension methods for <see cref="IManagedProjectDiagnosticOutputService"/> instances.
    /// </summary>
    internal static partial class ProjectDiagnosticOutputServiceExtensions
    {
        /// <summary>
        ///     If <see cref="IManagedProjectDiagnosticOutputService.IsEnabled"/> is <see langword="true"/>,
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
        public static void WriteLine(this IManagedProjectDiagnosticOutputService logger, string format, object? argument)
        {
            Requires.NotNull(logger, nameof(logger));

            if (logger.IsEnabled)
            {
                logger.WriteLine(string.Format(format, argument));
            }
        }

        /// <summary>
        ///     If <see cref="IManagedProjectDiagnosticOutputService.IsEnabled"/> is <see langword="true"/>,
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
        public static void WriteLine(this IManagedProjectDiagnosticOutputService logger, string format, object? argument1, object? argument2)
        {
            Requires.NotNull(logger, nameof(logger));

            if (logger.IsEnabled)
            {
                logger.WriteLine(string.Format(format, argument1, argument2));
            }
        }

        /// <summary>
        ///     If <see cref="IManagedProjectDiagnosticOutputService.IsEnabled"/> is <see langword="true"/>,
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
        public static void WriteLine(this IManagedProjectDiagnosticOutputService logger, string format, object? argument1, object? argument2, object? argument3)
        {
            Requires.NotNull(logger, nameof(logger));

            if (logger.IsEnabled)
            {
                logger.WriteLine(string.Format(format, argument1, argument2, argument3));
            }
        }

        /// <summary>
        ///     If <see cref="IManagedProjectDiagnosticOutputService.IsEnabled"/> is <see langword="true"/>,
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
        public static void WriteLine(this IManagedProjectDiagnosticOutputService logger, string format, params object?[] arguments)
        {
            Requires.NotNull(logger, nameof(logger));

            if (logger.IsEnabled)
            {
                logger.WriteLine(string.Format(format, arguments));
            }
        }
    }
}
