// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;

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
        private readonly object _snapshotProvidersLock = new object();

        [ImportingConstructor]
        public AggregateDependenciesSnapshotProvider(IProjectExportProvider projectExportProvider)
        {
            ProjectExportProvider = projectExportProvider;
        }

        private IProjectExportProvider ProjectExportProvider { get; }

        private ConcurrentDictionary<string, IDependenciesSnapshotProvider> SnapshotProviders { get; }
            = new ConcurrentDictionary<string, IDependenciesSnapshotProvider>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        public event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;

        public void RegisterSnapshotProvider(IDependenciesSnapshotProvider snapshotProvider)
        {
            if (snapshotProvider == null)
            {
                return;
            }

            lock (_snapshotProvidersLock)
            {
                SnapshotProviders[snapshotProvider.ProjectFilePath] = snapshotProvider;
                snapshotProvider.SnapshotRenamed += OnSnapshotRenamed;
                snapshotProvider.SnapshotChanged += OnSnapshotChanged;
                snapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;
            }
        }

        private void OnSnapshotRenamed(object sender, ProjectRenamedEventArgs e)
        {
            lock (_snapshotProvidersLock)
            {
                // remove and re-add provider with new project path
                if (!string.IsNullOrEmpty(e.OldFullPath)
                    && SnapshotProviders.TryRemove(e.OldFullPath, out IDependenciesSnapshotProvider provider)
                    && provider != null
                    && !string.IsNullOrEmpty(e.NewFullPath))
                {
                    SnapshotProviders[e.NewFullPath] = provider;
                }
            }
        }

        private void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            SnapshotChanged?.Invoke(this, e);
        }

        /// <summary>
        /// When a given project context is unloaded, remove it form the cache and unregister event handlers
        /// </summary>
        internal void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
        {
            IDependenciesSnapshotProvider snapshotProvider = e.SnapshotProvider;
            if (snapshotProvider == null)
            {
                return;
            }

            SnapshotProviderUnloading?.Invoke(this, e);

            lock (_snapshotProvidersLock)
            {
                SnapshotProviders.TryRemove(snapshotProvider.ProjectFilePath, out IDependenciesSnapshotProvider provider);
                snapshotProvider.SnapshotRenamed -= OnSnapshotRenamed;
                snapshotProvider.SnapshotChanged -= OnSnapshotChanged;
                snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
            }
        }

        public IDependenciesSnapshotProvider GetSnapshotProvider(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentException(nameof(projectFilePath));
            }

            lock (_snapshotProvidersLock)
            {
                if (SnapshotProviders.TryGetValue(projectFilePath, out IDependenciesSnapshotProvider snapshotProvider))
                {
                    return snapshotProvider;
                }

                snapshotProvider = ProjectExportProvider.GetExport<IDependenciesSnapshotProvider>(projectFilePath);
                if (snapshotProvider != null)
                {
                    RegisterSnapshotProvider(snapshotProvider);
                }

                return snapshotProvider;
            }
        }

        public IEnumerable<IDependenciesSnapshotProvider> GetSnapshotProviders()
        {
            lock (_snapshotProvidersLock)
            {
                return SnapshotProviders.Values;
            }
        }
    }
}
