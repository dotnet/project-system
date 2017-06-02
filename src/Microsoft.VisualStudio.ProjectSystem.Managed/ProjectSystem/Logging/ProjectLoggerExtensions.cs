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
    }
}
