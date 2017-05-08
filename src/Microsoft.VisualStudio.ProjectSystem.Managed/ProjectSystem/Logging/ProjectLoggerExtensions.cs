// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides static extension methods for <see cref="IProjectLogger"/> instances.
    /// </summary>
    internal static class ProjectLoggerExtensions
    {
        /// <summary>
        ///     Writes the sender and text representation of the specified array of objects,
        ///     followed by the current line terminator, to the log using the 
        ///     specified format information.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="logger"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="sender"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="format"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        public static void WriteLine(this IProjectLogger logger, object sender, string format, params object[] arguments)
        {
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(sender, nameof(sender));

            if (logger.IsEnabled)
            {
                logger.WriteLine($"{sender.GetType().Name}: {format}", arguments);
            }
        }
    }
}
