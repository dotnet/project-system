// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// Watches for save events on the given file.
    /// </summary>
    interface ITextBufferStateListener : IDisposable
    {
        /// <summary>
        /// Initializes the state listener for the given file.
        /// </summary>
        Task InitializeListenerAsync(string filePath);
    }
}
