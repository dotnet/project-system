// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     An <see cref="IProjectLogger"/> that batches up logging writes
    ///     and writes them all at once on <see cref="IDisposable.Dispose"/>.
    /// </summary>
    internal interface IProjectLoggerBatch : IProjectLogger, IDisposable
    {
        /// <summary>
        ///     Gets or sets the indent level.
        /// </summary>
        /// <value>
        ///     The indent level. The default is 0.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value"/> is less than 0.
        /// </exception>
        int IndentLevel
        {
            get;
            set;
        }
    }
}
