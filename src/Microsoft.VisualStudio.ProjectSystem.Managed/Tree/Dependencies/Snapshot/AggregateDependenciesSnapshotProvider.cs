// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
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
        public event EventHandler<SnapshotChangedEventArgs>? SnapshotChanged;

        /// <inheritdoc />
        public event EventHandler<SnapshotProviderUnloadingEventArgs>? SnapshotProviderUnloading;

        /// <inheritdoc />
        public IDisposable RegisterSnapshotProvider(IDependenciesSnapshotProvider snapshotProvider)
        {
            Requires.NotNull(snapshotProvider, nameof(snapshotProvider));

            var unregister = new DisposableBag();

            lock (_snapshotProviders)
            {
                snapshotProvider.SnapshotRenamed += OnSnapshotRenamed;
                snapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;

                ITargetBlock<SnapshotChangedEventArgs> actionBlock = DataflowBlockSlim.CreateActionBlock<SnapshotChangedEventArgs>(
                    e => SnapshotChanged?.Invoke(this, e),
                    "AggregateDependenciesSnapshotProviderSource {1}",
                    skipIntermediateInputData: true);

                unregister.Add(
                    snapshotProvider.SnapshotChangedSource.LinkTo(
                        actionBlock,
                        DataflowOption.PropagateCompletion));

                _snapshotProviders[snapshotProvider.CurrentSnapshot.ProjectPath] = snapshotProvider;
            }

            unregister.Add(new DisposableDelegate(
                () =>
                {
                    lock (_snapshotProviders)
                    {
                        string projectPath = snapshotProvider.CurrentSnapshot.ProjectPath;
                        _snapshotProviders.Remove(projectPath);
                        snapshotProvider.SnapshotRenamed -= OnSnapshotRenamed;
                        snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
                    }
                }));

            return unregister;

            void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                // Project has unloaded, so remove it from the cache and unregister event handlers
                SnapshotProviderUnloading?.Invoke(this, e);

                unregister.Dispose();
            }

            void OnSnapshotRenamed(object sender, ProjectRenamedEventArgs e)
            {
                lock (_snapshotProviders)
                {
                    // Remove and re-add provider with new project path
                    if (!string.IsNullOrEmpty(e.OldFullPath)
                        && _snapshotProviders.TryGetValue(e.OldFullPath, out IDependenciesSnapshotProvider provider)
                        && _snapshotProviders.Remove(e.OldFullPath)
                        && !string.IsNullOrEmpty(e.NewFullPath))
                    {
                        _snapshotProviders[e.NewFullPath] = provider;
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
                _snapshotProviders.TryGetValue(projectFilePath, out IDependenciesSnapshotProvider? provider);

                return provider?.CurrentSnapshot;
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
                return _snapshotProviders.Values.Select(provider => provider.CurrentSnapshot).ToList();
            }
        }
    }
}
