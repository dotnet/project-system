// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
    {
        private readonly Dictionary<string, IDependenciesSnapshotProvider> _snapshotProviders = new Dictionary<string, IDependenciesSnapshotProvider>(StringComparer.OrdinalIgnoreCase);
        private readonly IProjectExportProvider _projectExportProvider;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public AggregateDependenciesSnapshotProvider(
            IProjectExportProvider projectExportProvider,
            ITargetFrameworkProvider targetFrameworkProvider)
        {
            _projectExportProvider = projectExportProvider;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        /// <inheritdoc />
        public event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        /// <inheritdoc />
        public event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;

        /// <inheritdoc />
        public void RegisterSnapshotProvider(IDependenciesSnapshotProvider snapshotProvider)
        {
            if (snapshotProvider == null)
            {
                return;
            }

            lock (_snapshotProviders)
            {
                _snapshotProviders[snapshotProvider.CurrentSnapshot.ProjectPath] = snapshotProvider;
                snapshotProvider.SnapshotRenamed += OnSnapshotRenamed;
                snapshotProvider.SnapshotChanged += OnSnapshotChanged;
                snapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;
            }

            // When a given project context is unloaded, remove it from the cache and unregister event handlers
            void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                SnapshotProviderUnloading?.Invoke(this, e);

                lock (_snapshotProviders)
                {
                    _snapshotProviders.Remove(snapshotProvider.CurrentSnapshot.ProjectPath);
                    snapshotProvider.SnapshotRenamed -= OnSnapshotRenamed;
                    snapshotProvider.SnapshotChanged -= OnSnapshotChanged;
                    snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
                }
            }

            void OnSnapshotRenamed(object sender, ProjectRenamedEventArgs e)
            {
                lock (_snapshotProviders)
                {
                    // Remove and re-add provider with new project path
                    if (!string.IsNullOrEmpty(e.OldFullPath)
                        && _snapshotProviders.TryGetValue(e.OldFullPath, out IDependenciesSnapshotProvider provider)
                        && _snapshotProviders.Remove(e.OldFullPath)
                        && provider != null
                        && !string.IsNullOrEmpty(e.NewFullPath))
                    {
                        _snapshotProviders[e.NewFullPath] = provider;
                    }
                }
            }

            void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
            {
                SnapshotChanged?.Invoke(this, e);
            }
        }

        /// <inheritdoc />
        public IDependenciesSnapshot GetSnapshot(string projectFilePath)
        {
            Requires.NotNullOrEmpty(projectFilePath, nameof(projectFilePath));

            lock (_snapshotProviders)
            {
                if (!_snapshotProviders.TryGetValue(projectFilePath, out IDependenciesSnapshotProvider snapshotProvider))
                {
                    snapshotProvider = _projectExportProvider.GetExport<IDependenciesSnapshotProvider>(projectFilePath);

                    if (snapshotProvider != null)
                    {
                        RegisterSnapshotProvider(snapshotProvider);
                    }
                }

                return snapshotProvider?.CurrentSnapshot;
            }
        }

        public ITargetedDependenciesSnapshot GetSnapshot(IDependency dependency)
        {
            IDependenciesSnapshot snapshot = GetSnapshot(dependency.FullPath);

            if (snapshot == null)
            {
                return null;
            }

            ITargetFramework targetFramework = _targetFrameworkProvider.GetNearestFramework(
                dependency.TargetFramework, snapshot.Targets.Keys);

            if (targetFramework == null)
            {
                return null;
            }

            return snapshot.Targets[targetFramework];
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IDependenciesSnapshot> GetSnapshots()
        {
            lock (_snapshotProviders)
            {
                return _snapshotProviders.Values.Select(p => p.CurrentSnapshot).ToList();
            }
        }
    }
}
