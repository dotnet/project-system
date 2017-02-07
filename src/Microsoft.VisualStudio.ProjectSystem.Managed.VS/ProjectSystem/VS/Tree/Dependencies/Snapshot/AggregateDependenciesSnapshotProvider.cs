// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies graph contexts.
    /// </summary>
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
    {
        [ImportingConstructor]
        public AggregateDependenciesSnapshotProvider(IProjectExportProvider projectExportProvider,
                                                     IProjectServiceAccessor projectServiceAccessor)
        {
            ProjectServiceAccessor = projectServiceAccessor;
            ProjectExportProvider = projectExportProvider;
        }

        private IProjectExportProvider ProjectExportProvider { get; }

        private IProjectServiceAccessor ProjectServiceAccessor { get; }

        private ConcurrentDictionary<string, IDependenciesSnapshotProvider> SnapshotProviders { get; }
                            = new ConcurrentDictionary<string, IDependenciesSnapshotProvider>
                                        (StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        public event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;

        internal void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            SnapshotChanged?.Invoke(this, e);
        }

        /// <summary>
        /// When a given project context is unloaded, remove it form the cache and unregister event handlers
        /// </summary>
        internal void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
        {
            var snapshotProvider = e.SnapshotProvider;
            if (snapshotProvider == null)
            {
                return;
            }

            SnapshotProviderUnloading?.Invoke(this, e);

            // Remove context for the unloaded project from the cache
            SnapshotProviders.TryRemove(snapshotProvider.ProjectFilePath, out IDependenciesSnapshotProvider provider);

            snapshotProvider.SnapshotChanged -= OnSnapshotChanged;
            snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
        }

        public IDependenciesSnapshotProvider GetSnapshotProvider(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentException(nameof(projectFilePath));
            }

            if (SnapshotProviders.TryGetValue(projectFilePath, out IDependenciesSnapshotProvider context))
            {
                return context;
            }

            context = ProjectExportProvider.GetExport<IDependenciesSnapshotProvider>(projectFilePath);
            if (context == null)
            {
                return null;
            }

            SnapshotProviders[projectFilePath] = context;
            context.SnapshotChanged += OnSnapshotChanged;
            context.SnapshotProviderUnloading += OnSnapshotProviderUnloading;

            return context;
        }

        public IEnumerable<IDependenciesSnapshotProvider> GetSnapshotProviders()
        {
            var projectService = ProjectServiceAccessor.GetProjectService();
            if (projectService == null)
            {
                return null;
            }

            return GetSnapshotProvidersInternal(projectService);
        }

        internal IEnumerable<IDependenciesSnapshotProvider> GetSnapshotProvidersInternal(
                    IProjectService projectService)
        {
            var projects = projectService.LoadedUnconfiguredProjects;
            foreach (var project in projects)
            {
                var context = GetSnapshotProvider(project.FullPath);
                if (context != null)
                {
                    yield return context;
                }
            }
        }
    }
}
