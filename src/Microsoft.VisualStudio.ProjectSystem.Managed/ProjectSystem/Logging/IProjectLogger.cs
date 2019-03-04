// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        bool IsEnabled
        {
            get;
        }

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
