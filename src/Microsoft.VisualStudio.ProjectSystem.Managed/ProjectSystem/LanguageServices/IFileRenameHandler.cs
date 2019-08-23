// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Rename
{
    /// <summary>
    /// General abstraction of type that needs to be notified when a file rename occurs in the project system
    /// </summary>
    internal interface IFileRenameHandler
    {
        /// <summary>
        /// Notifies the handler that the given file has been renamed from oldFilePath to newFilePath.
        /// </summary>
        /// <param name="oldFilePath">The original path and name of the file</param>
        /// <param name="newFilePath">The new path and name of the file</param>
        void HandleRename(string oldFilePath, string newFilePath);
    }
}
