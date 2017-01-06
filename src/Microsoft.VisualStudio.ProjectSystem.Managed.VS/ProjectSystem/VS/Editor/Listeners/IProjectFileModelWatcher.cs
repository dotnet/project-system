// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// Watches for changes to the project file model maintained by VS, notifying <see cref="IProjectFileEditorPresenter"/>
    /// when updates are made.
    /// </summary>
    internal interface IProjectFileModelWatcher : IDisposable
    {
        /// <summary>
        /// Starts the event watching.
        /// </summary>
        void InitializeModelWatcher();
    }
}
