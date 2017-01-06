// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface ITextBufferManager: IDisposable
    {
        /// <summary>
        /// Gets the file path of the file being managed by this manager.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Initializes the buffer, performing any necessary setup. This must be called prior to other methods.
        /// </summary>
        Task InitializeBufferAsync();

        /// <summary>
        /// Sets the buffer to be ReadOnly, preventing user input to the buffer.
        /// </summary>
        Task SetReadOnlyAsync(bool readOnly);

        /// <summary>
        /// Resets the content of the buffer, initializing it to the content of the msbuild model.
        /// </summary>
        Task ResetBufferAsync();

        /// <summary>
        /// Saves the current content of the buffer to the project.
        /// </summary>
        Task SaveAsync();
    }
}
