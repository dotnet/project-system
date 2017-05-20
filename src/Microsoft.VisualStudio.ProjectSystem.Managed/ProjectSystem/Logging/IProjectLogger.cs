// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides methods for logging project messages.
    /// </summary>
    internal interface IProjectLogger
    {
        /// <summary>
        ///     Writes the specified text, followed by the current line terminator, 
        ///     to the log.
        /// </summary>
        void WriteLine(string text);
    }
}