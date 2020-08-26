// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides methods for logging project messages.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectLogger
    {
        /// <summary>
        ///     Gets a value indicating if the logger is enabled.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if the <see cref="IProjectLogger"/> is
        ///     enabled and logging to the log; otherwise, <see langword="false"/>.
        /// </value>
        bool IsEnabled { get; }

        /// <summary>
        ///     If <see cref="IsEnabled"/> is <see langword="true"/>, writes
        ///     the text representation of the format, followed by the current
        ///     line terminator
        /// </summary>
        /// <exception cref="FormatException">
        ///     <see cref="IsEnabled"/> is <see langword="true"/> and the format
        ///     specification in <paramref name="format"/> is invalid.
        /// </exception>
        void WriteLine(in StringFormat format);
    }
}
