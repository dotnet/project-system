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
        private readonly Dictionary<string, IDependenciesSnapshotProvider> _snapshotProviderByProjectPath = new Dictionary<string, IDependenciesSnapshotProvider>(StringComparer.OrdinalIgnoreCase);
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

            lock (_snapshotProviderByProjectPath)
            {
                _snapshotProviderByProjectPath[snapshotProvider.CurrentSnapshot.ProjectPath] = snapshotProvider;
                snapshotProvider.SnapshotRenamed += OnSnapshotRenamed;
                snapshotProvider.SnapshotChanged += OnSnapshotChanged;
                snapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;
            }

            // When a given project context is unloaded, remove it from the cache and unregister event handlers
            void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                SnapshotProviderUnloading?.Invoke(this, e);

                lock (_snapshotProviderByProjectPath)
                {
                    _snapshotProviderByProjectPath.Remove(snapshotProvider.CurrentSnapshot.ProjectPath);
                    snapshotProvider.SnapshotRenamed -= OnSnapshotRenamed;
                    snapshotProvider.SnapshotChanged -= OnSnapshotChanged;
                    snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
                }
            }

            void OnSnapshotRenamed(object sender, ProjectRenamedEventArgs e)
            {
                lock (_snapshotProviderByProjectPath)
                {
                    // Remove and re-add provider with new project path
                    if (!string.IsNullOrEmpty(e.OldFullPath)
                        && _snapshotProviderByProjectPath.TryGetValue(e.OldFullPath, out IDependenciesSnapshotProvider provider)
                        && _snapshotProviderByProjectPath.Remove(e.OldFullPath)
                        && provider != null
                        && !string.IsNullOrEmpty(e.NewFullPath))
                    {
                        _snapshotProviderByProjectPath[e.NewFullPath] = provider;
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

            lock (_snapshotProviderByProjectPath)
            {
                if (!_snapshotProviderByProjectPath.TryGetValue(projectFilePath, out IDependenciesSnapshotProvider snapshotProvider))
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
            lock (_snapshotProviderByProjectPath)
            {
                return _snapshotProviderByProjectPath.Values.Select(p => p.CurrentSnapshot).ToList();
            }
        }
    }
}
