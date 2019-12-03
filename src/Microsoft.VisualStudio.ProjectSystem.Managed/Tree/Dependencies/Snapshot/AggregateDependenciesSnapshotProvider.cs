// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc cref="IAggregateDependenciesSnapshotProvider"/>
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
    {
        /// <summary>
        /// Even though the collection is immutable we still lock to ensure synchronized event subscription and unsubscription.
        /// Because <see cref="AggregateDependenciesSnapshotProvider"/> is in global scope, this is a global lock.
        /// </summary>
        private readonly object _lock = new object();

        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        /// <summary>
        /// Immutable map from project path to snapshot provider.
        /// </summary>
        /// <remarks>
        /// Modifications of this collection are locked by <see cref="_lock"/>, however we still use an immutable collection
        /// here so that read-only calls from <see cref="GetSnapshot(string)"/>, <see cref="GetSnapshot(IDependency)"/> and
        /// <see cref="GetSnapshots"/> don't need to take a global lock.
        /// </remarks>
        private ImmutableDictionary<string, DependenciesSnapshotProvider> _snapshotProviderByPath;

        [ImportingConstructor]
        public AggregateDependenciesSnapshotProvider(ITargetFrameworkProvider targetFrameworkProvider)
        {
            _targetFrameworkProvider = targetFrameworkProvider;

            _snapshotProviderByPath = ImmutableDictionary<string, DependenciesSnapshotProvider>.Empty.WithComparers(StringComparers.Paths);
        }

        public event EventHandler<SnapshotChangedEventArgs>? SnapshotChanged;

        public event EventHandler<SnapshotProviderUnloadingEventArgs>? SnapshotProviderUnloading;

        public IDisposable RegisterSnapshotProvider(DependenciesSnapshotProvider snapshotProvider)
        {
            Requires.NotNull(snapshotProvider, nameof(snapshotProvider));

            var unregister = new DisposableBag();

            lock (_lock)
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

                _snapshotProviderByPath = _snapshotProviderByPath.SetItem(snapshotProvider.CurrentSnapshot.ProjectPath, snapshotProvider);
            }

            unregister.Add(new DisposableDelegate(
                () =>
                {
                    lock (_lock)
                    {
                        string projectPath = snapshotProvider.CurrentSnapshot.ProjectPath;
                        _snapshotProviderByPath = _snapshotProviderByPath.Remove(projectPath);
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
                if (string.IsNullOrEmpty(e.OldFullPath))
                {
                    return;
                }

                lock (_lock)
                {
                    // Remove and re-add provider with new project path
                    if (_snapshotProviderByPath.TryGetValue(e.OldFullPath, out DependenciesSnapshotProvider provider))
                    {
                        _snapshotProviderByPath = _snapshotProviderByPath.Remove(e.OldFullPath);

                        if (!string.IsNullOrEmpty(e.NewFullPath))
                        {
                            _snapshotProviderByPath = _snapshotProviderByPath.SetItem(e.NewFullPath, provider);
                        }
                    }
                }
            }
        }

        public DependenciesSnapshot? GetSnapshot(string projectFilePath)
        {
            Requires.NotNullOrEmpty(projectFilePath, nameof(projectFilePath));

            _snapshotProviderByPath.TryGetValue(projectFilePath, out DependenciesSnapshotProvider? provider);

            return provider?.CurrentSnapshot;
        }

        public TargetedDependenciesSnapshot? GetSnapshot(IDependency dependency)
        {
            DependenciesSnapshot? snapshot = GetSnapshot(dependency.FullPath);

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

        public IReadOnlyCollection<DependenciesSnapshot> GetSnapshots()
        {
            return _snapshotProviderByPath.Values.Select(provider => provider.CurrentSnapshot).ToList();
        }
    }
}
