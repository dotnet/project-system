// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        int IndentLevel { get; set; }
    }
}
