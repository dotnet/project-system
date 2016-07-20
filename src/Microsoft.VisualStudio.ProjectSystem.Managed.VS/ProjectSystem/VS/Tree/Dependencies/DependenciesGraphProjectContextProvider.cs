// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies graph contexts.
    /// </summary>
    [Export(typeof(IDependenciesGraphProjectContextProvider))]
    [AppliesTo(ProjectCapability.AlwaysAvailable)]
    public class DependenciesGraphProjectContextProvider : IDependenciesGraphProjectContextProvider
    {
        private IProjectExportProvider ProjectExportProvider { get; set; }

        private SVsServiceProvider ServiceProvider { get; set; }

        private Dictionary<string, IDependenciesGraphProjectContext> ProjectContexts { get; } 
                            = new Dictionary<string, IDependenciesGraphProjectContext>(StringComparer.OrdinalIgnoreCase);

        private object _projectContextsLock = new object();

        [ImportingConstructor]
        public DependenciesGraphProjectContextProvider(IProjectExportProvider projectExportProvider, 
                                                       SVsServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            ProjectExportProvider = projectExportProvider;
        }

        /// <summary>
        /// When project context changed event received from any context, send a global level
        /// "context changed" event, to notify  <see cref="DependenciesGraphProvider"/>.
        /// </summary>
        internal void OnProjectContextChanged(object sender, ProjectContextEventArgs e)
        {
            var context = e.Context;
            if (context == null)
            {
                return;
            }

            ProjectContextChanged?.Invoke(this, new ProjectContextEventArgs(context));
        }

        /// <summary>
        /// When a given project context is unloaded, remove it form the cache and unregister event handlers
        /// </summary>
        internal void OnProjectContextUnloaded(object sender, ProjectContextEventArgs e)
        {
            lock (_projectContextsLock)
            {
                var context = e.Context;
                if (context != null && ProjectContexts.ContainsKey(context.ProjectFilePath))
                {
                    // Remove context for the unloaded project from the cache
                    ProjectContexts.Remove(context.ProjectFilePath);

                    context.ProjectContextChanged -= OnProjectContextChanged;
                    context.ProjectContextUnloaded -= OnProjectContextUnloaded;
                }
            }
        }

        #region IDependenciesGraphProjectContextProvider

        /// <summary>
        /// Returns an unconfigured project level contexts for given project file path.
        /// </summary>
        /// <param name="projectFilePath">Full path to project path.</param>
        /// <returns>
        /// Instance of <see cref="IDependenciesGraphProjectContext"/> or null if context was not found for given project file.
        /// </returns>
        public IDependenciesGraphProjectContext GetProjectContext(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                return null;
            }

            lock (_projectContextsLock)
            {
                if (ProjectContexts.ContainsKey(projectFilePath))
                {
                    return ProjectContexts[projectFilePath];
                }
            }

            var context = ProjectExportProvider.GetExport<IDependenciesGraphProjectContext>(projectFilePath);
            if (context == null)
            {
                return null;
            }

            lock (_projectContextsLock)
            {
                ProjectContexts[projectFilePath] = context;
                context.ProjectContextChanged += OnProjectContextChanged;
                context.ProjectContextUnloaded += OnProjectContextUnloaded;
            }

            return context;
        }

        public IEnumerable<IDependenciesGraphProjectContext> GetProjectContexts()
        {
            var projects = ActiveProjectTracker.Instance.GetAllProjects();
            foreach (var project in projects)
            {
                yield return GetProjectContext(project.FullPath);
            }
        }

        /// <summary>
        /// Gets called when context (projec dependencies) change
        /// </summary>
        public event ProjectContextEventHandler ProjectContextChanged;

        #endregion
    }
}
