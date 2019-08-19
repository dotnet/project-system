// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
    {
        private readonly Dictionary<string, IDependenciesSnapshotProvider> _snapshotProviderByPath = new Dictionary<string, IDependenciesSnapshotProvider>(StringComparer.OrdinalIgnoreCase);
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public AggregateDependenciesSnapshotProvider(ITargetFrameworkProvider targetFrameworkProvider)
        {
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

            lock (_snapshotProviderByPath)
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

                _snapshotProviderByPath[snapshotProvider.CurrentSnapshot.ProjectPath] = snapshotProvider;
            }

            unregister.Add(new DisposableDelegate(
                () =>
                {
                    lock (_snapshotProviderByPath)
                    {
                        string projectPath = snapshotProvider.CurrentSnapshot.ProjectPath;
                        _snapshotProviderByPath.Remove(projectPath);
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
                lock (_snapshotProviderByPath)
                {
                    // Remove and re-add provider with new project path
                    if (!string.IsNullOrEmpty(e.OldFullPath)
                        && _snapshotProviderByPath.TryGetValue(e.OldFullPath, out IDependenciesSnapshotProvider provider)
                        && _snapshotProviderByPath.Remove(e.OldFullPath)
                        && !string.IsNullOrEmpty(e.NewFullPath))
                    {
                        _snapshotProviderByPath[e.NewFullPath] = provider;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IDependenciesSnapshot? GetSnapshot(string projectFilePath)
        {
            Requires.NotNullOrEmpty(projectFilePath, nameof(projectFilePath));

            lock (_snapshotProviderByPath)
            {
                _snapshotProviderByPath.TryGetValue(projectFilePath, out IDependenciesSnapshotProvider? provider);

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
            lock (_snapshotProviderByPath)
            {
                return _snapshotProviderByPath.Values.Select(provider => provider.CurrentSnapshot).ToList();
            }
        }
    }
}
