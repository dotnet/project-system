// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// Watches for when a frame is closed or the project is unloaded, updating the <see cref="IProjectFileEditorPresenter"/> of
    /// these events.
    /// </summary>
    internal interface IFrameOpenCloseListener
    {
        /// <summary>
        /// Initialize the listener to watch for events that happen to the given frame.
        /// </summary>
        Task InitializeEventsAsync(IVsWindowFrame frame);

        /// <summary>
        /// Disposes of the listener, stopping any future event notifications.
        /// </summary>
        Task DisposeAsync();
    }
}
