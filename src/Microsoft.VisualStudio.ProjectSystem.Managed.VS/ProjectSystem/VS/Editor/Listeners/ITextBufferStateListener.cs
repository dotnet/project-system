// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor.Listeners
{
    /// <summary>
    /// Manages the state of the text buffer an associated window, updating the dirty *, and handling save events.
    /// </summary>
    interface ITextBufferStateListener : IDisposable
    {
        /// <summary>
        /// Initializes the state listener for the given window frame host.
        /// </summary>
        Task InitializeAsync(WindowPane hostFrame);

        /// <summary>
        /// Saves the content of the textLines buffer.
        /// </summary>
        /// <returns></returns>
        Task SaveAsync();

        /// <summary>
        /// Forces the text buffer to consider the current state to be unedited.
        /// </summary>
        Task ForceBufferStateCleanAsync();
    }
}
