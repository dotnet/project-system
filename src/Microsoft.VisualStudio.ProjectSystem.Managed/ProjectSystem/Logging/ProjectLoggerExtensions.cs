// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides static extension methods for <see cref="IProjectLogger"/> instances.
    /// </summary>
    internal static class ProjectLoggerExtensions
    {
        /// <summary>
        ///     Begins a logger context the specified text.
        /// </summary>
        public static ProjectLoggerContext BeginContext(this IProjectLogger logger, string text)
        {
            Requires.NotNull(logger, nameof(logger));

            return new ProjectLoggerContext(logger, new FormatArray(text));
        }

        /// <summary>
        ///     Begins a logger context with the specified object and format information.
        /// </summary>
        public static ProjectLoggerContext BeginContext(this IProjectLogger logger, string format, object argument)
        {
            Requires.NotNull(logger, nameof(logger));

            return new ProjectLoggerContext(logger, new FormatArray(format, argument));
        }

        /// <summary>
        ///     Begins a logger context with the specified objects and format information.
        /// </summary>
        public static ProjectLoggerContext BeginContext(this IProjectLogger logger, string format, object argument1, object argument2)
        {
            Requires.NotNull(logger, nameof(logger));

            return new ProjectLoggerContext(logger, new FormatArray(format, argument1, argument2));
        }

        /// <summary>
        ///     Begins a logger context with the specified objects and format information.
        /// </summary>
        public static ProjectLoggerContext BeginContext(this IProjectLogger logger, string format, object argument1, object argument2, object argument3)
        {
            Requires.NotNull(logger, nameof(logger));

            return new ProjectLoggerContext(logger, new FormatArray(format, argument1, argument2, argument3));
        }

        /// <summary>
        ///     Begins a logger context with the specified array of objects and format information.
        /// </summary>
        public static ProjectLoggerContext BeginContext(this IProjectLogger logger, string format, params object[] arguments)
        {
            Requires.NotNull(logger, nameof(logger));

            return new ProjectLoggerContext(logger, new FormatArray(format, arguments));
        }
    }
}
