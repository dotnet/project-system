// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies graph contexts.
    /// </summary>
    internal interface IDependenciesGraphProjectContextProvider
    {
        /// <summary>
        /// Returns an unconfigured project level contexts for given project file path.
        /// </summary>
        /// <param name="projectFilePath">Full path to project path.</param>
        /// <returns>
        /// Instance of <see cref="IDependenciesGraphProjectContext"/> or null if context was not found for given project file.
        /// </returns>
        IDependenciesGraphProjectContext GetProjectContext(string projectFilePath);

        IEnumerable<IDependenciesGraphProjectContext> GetProjectContexts();

        /// <summary>
        /// Gets called when dependencies change
        /// </summary>
        event EventHandler<ProjectContextEventArgs> ProjectContextChanged;

        /// <summary>
        /// Gets called when project unloads to notify GraphProvider to release
        /// any data associated with the project.
        /// </summary>
        event EventHandler<ProjectContextEventArgs> ProjectContextUnloaded;
    }
}
