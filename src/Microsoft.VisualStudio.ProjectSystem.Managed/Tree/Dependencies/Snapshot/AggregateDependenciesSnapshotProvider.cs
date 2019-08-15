// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
    {
        private readonly Dictionary<string, (IDependenciesSnapshotProvider Provider, IDisposable Subscription)> _snapshotProviders = new Dictionary<string, (IDependenciesSnapshotProvider, IDisposable)>(StringComparer.OrdinalIgnoreCase);
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
        public event EventHandler<SnapshotChangedEventArgs>? SnapshotChanged;

        /// <inheritdoc />
        public event EventHandler<SnapshotProviderUnloadingEventArgs>? SnapshotProviderUnloading;

        /// <inheritdoc />
        public void RegisterSnapshotProvider(IDependenciesSnapshotProvider snapshotProvider)
        {
            Requires.NotNull(snapshotProvider, nameof(snapshotProvider));

            lock (_snapshotProviders)
            {
                snapshotProvider.SnapshotRenamed += OnSnapshotRenamed;
                snapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;

                ITargetBlock<SnapshotChangedEventArgs> actionBlock = DataflowBlockSlim.CreateActionBlock<SnapshotChangedEventArgs>(
                    e => SnapshotChanged?.Invoke(this, e),
                    "AggregateDependenciesSnapshotProviderSource {1}",
                    skipIntermediateInputData: true);
                IDisposable subscription = snapshotProvider.SnapshotChangedSource.LinkTo(actionBlock, DataflowOption.PropagateCompletion);

                _snapshotProviders[snapshotProvider.CurrentSnapshot.ProjectPath] = (snapshotProvider, subscription);
            }

            void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                // Project has unloaded, so remove it from the cache and unregister event handlers
                SnapshotProviderUnloading?.Invoke(this, e);

                lock (_snapshotProviders)
                {
                    string projectPath = snapshotProvider.CurrentSnapshot.ProjectPath;
                    bool found = _snapshotProviders.TryGetValue(projectPath, out (IDependenciesSnapshotProvider Provider, IDisposable Subscription) entry);
                    Assumes.True(found);
                    _snapshotProviders.Remove(projectPath);
                    snapshotProvider.SnapshotRenamed -= OnSnapshotRenamed;
                    snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
                    entry.Subscription.Dispose();
                }
            }

            void OnSnapshotRenamed(object sender, ProjectRenamedEventArgs e)
            {
                lock (_snapshotProviders)
                {
                    // Remove and re-add provider with new project path
                    if (!string.IsNullOrEmpty(e.OldFullPath)
                        && _snapshotProviders.TryGetValue(e.OldFullPath, out (IDependenciesSnapshotProvider Provider, IDisposable Subscription) entry)
                        && _snapshotProviders.Remove(e.OldFullPath)
                        && !string.IsNullOrEmpty(e.NewFullPath))
                    {
                        _snapshotProviders[e.NewFullPath] = entry;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IDependenciesSnapshot? GetSnapshot(string projectFilePath)
        {
            Requires.NotNullOrEmpty(projectFilePath, nameof(projectFilePath));

            lock (_snapshotProviders)
            {
                IDependenciesSnapshotProvider? snapshotProvider;

                if (_snapshotProviders.TryGetValue(projectFilePath, out (IDependenciesSnapshotProvider Provider, IDisposable Subscription) entry))
                {
                    snapshotProvider = entry.Provider;
                }
                else
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

        /// <inheritdoc />
        public ITargetedDependenciesSnapshot? GetSnapshot(IDependency dependency)
        {
            IDependenciesSnapshot? snapshot = GetSnapshot(dependency.FullPath);

            if (snapshot == null)
            {
                return null;
            }

            ITargetFramework? targetFramework = _targetFrameworkProvider.GetNearestFramework(
                dependency.TargetFramework, snapshot.DependenciesByTargetFramework.Keys);

            if (targetFramework == null)
            {
                return null;
            }

            return snapshot.DependenciesByTargetFramework[targetFramework];
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IDependenciesSnapshot> GetSnapshots()
        {
            lock (_snapshotProviders)
            {
                return _snapshotProviders.Values.Select(p => p.Provider.CurrentSnapshot).ToList();
            }
        }
    }
}
