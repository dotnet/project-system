// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Contract for communication between unconfigured project level <see cref="DependenciesProjectTreeProvider"/> 
    /// and global scope level <see cref="DependenciesGraphProvider"/>, where DependenciesNodeProjectTreeProvider
    /// initialize Dependencies node and implements IDependenciesGraphProjectContext to allow 
    /// DependenciesNodeGraphProvider to access dependencies data for given project. That means that global Dependencies 
    /// node and it's first children sub nodes associated with dependency types are regular IProjectTree nodes, but 
    /// then actual dependencies under them will be dynamic graph nodes for perf reasons.
    /// </summary>
    public interface IDependenciesGraphProjectContext
    {
        /// <summary>
        /// Returns a dependencies node sub tree provider for given dependency provider type.
        /// </summary>
        /// <param name="providerType">
        /// Type of the dependnecy. It is expected to be a unique string associated with a provider. 
        /// </param>
        /// <returns>
        /// Instance of <see cref="IProjectDependenciesSubTreeProvider"/> or null if there no provider 
        /// for given type.
        /// </returns>
        IProjectDependenciesSubTreeProvider GetProvider(string providerType);

        IEnumerable<IProjectDependenciesSubTreeProvider> GetProviders();

        /// <summary>
        /// Path to project file
        /// </summary>
        string ProjectFilePath { get; }

        /// <summary>
        /// Gets called when dependencies change
        /// </summary>
        event ProjectContextEventHandler ProjectContextChanged;

        /// <summary>
        /// Gets called when project is unloading and dependencies subtree is disposing
        /// </summary>
        event ProjectContextEventHandler ProjectContextUnloaded;
    }

    public delegate void ProjectContextEventHandler(object sender, ProjectContextEventArgs e);

    public class ProjectContextEventArgs : EventArgs
    {
        public ProjectContextEventArgs(IDependenciesGraphProjectContext context)
        {
            Context = context;
        }

        public IDependenciesGraphProjectContext Context { get; }
    }
}
