// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface ITextBufferManager
    {
        /// <summary>
        /// Gets the buffer instance being used for the in-memory project file model.
        /// </summary>
        IVsTextLines TextLines { get; }

        /// <summary>
        /// Gets the underlying ITextBuffer for the IVsTextLines data.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Sets the buffer to be ReadOnly, preventing user input to the buffer.
        /// </summary>
        Task SetReadOnlyAsync(bool readOnly);

        /// <summary>
        /// Resets the content of the buffer, initializing it to the content of the msbuild model.
        /// </summary>
        void ResetBuffer();
    }
}
