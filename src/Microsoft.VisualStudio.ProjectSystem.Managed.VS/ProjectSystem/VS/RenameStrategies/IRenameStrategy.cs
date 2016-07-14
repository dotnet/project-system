// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.RenameStrategies
{
    /// <summary>
    /// General abstraction of rename strategies, allowing the Renamer to decide what type of renaming strategy is appropriate
    /// for the rename situation.
    /// </summary>
    internal interface IRenameStrategy
    {
        /// <summary>
        /// Returns whether or not this strategy applies to this rename. This should depend soley on the input and output names.
        /// </summary>
        /// <param name="oldFilePath">The original path and name of the file</param>
        /// <param name="newFilePath">The new path and name of the file</param>
        /// <returns>True if the strategy is applicable. False otherwise.</returns>
        bool CanHandleRename(string oldFilePath, string newFilePath);

        /// <summary>
        /// Performs refactors to the given project, given that a file is being renamed from oldFilePath to newFilePath.
        /// </summary>
        /// <param name="newProject">The project to rename</param>
        /// <param name="oldFilePath">The path to the old file location</param>
        /// <param name="newFilePath">The path to the new file location</param>
        Task RenameAsync(Project newProject, string oldFilePath, string newFilePath);
    }
}
