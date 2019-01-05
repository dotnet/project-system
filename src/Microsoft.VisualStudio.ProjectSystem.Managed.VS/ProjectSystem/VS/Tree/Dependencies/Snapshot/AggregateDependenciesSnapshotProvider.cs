// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <inheritdoc />
    [Export(typeof(IAggregateDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class AggregateDependenciesSnapshotProvider : IAggregateDependenciesSnapshotProvider
    {
        private ImmutableDictionary<string, IDependenciesSnapshotProvider> _snapshotProviderByProjectPath
            = ImmutableDictionary.Create<string, IDependenciesSnapshotProvider>(StringComparer.OrdinalIgnoreCase);

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

            ImmutableInterlocked.Update(
                ref _snapshotProviderByProjectPath,
                (dic, provider) => dic.SetItem(provider.CurrentSnapshot.ProjectPath, provider),
                snapshotProvider);

            snapshotProvider.SnapshotRenamed += OnSnapshotRenamed;
            snapshotProvider.SnapshotChanged += OnSnapshotChanged;
            snapshotProvider.SnapshotProviderUnloading += OnSnapshotProviderUnloading;

            // When a given project context is unloaded, remove it from the cache and unregister event handlers
            void OnSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                SnapshotProviderUnloading?.Invoke(this, e);

                ImmutableInterlocked.Update(
                    ref _snapshotProviderByProjectPath,
                    (dic, projectPath) => dic.Remove(projectPath),
                    snapshotProvider.CurrentSnapshot.ProjectPath);

                snapshotProvider.SnapshotRenamed -= OnSnapshotRenamed;
                snapshotProvider.SnapshotChanged -= OnSnapshotChanged;
                snapshotProvider.SnapshotProviderUnloading -= OnSnapshotProviderUnloading;
            }

            void OnSnapshotRenamed(object sender, ProjectRenamedEventArgs e)
            {
                // Remove and re-add provider with new project path
                ImmutableInterlocked.Update(
                    ref _snapshotProviderByProjectPath,
                    (dic, args) =>
                    {
                        if (dic.TryGetValue(args.OldFullPath, out IDependenciesSnapshotProvider provider))
                        {
                            dic = dic.Remove(args.OldFullPath);
                            if (!string.IsNullOrEmpty(args.NewFullPath))
                            {
                                dic = dic.SetItem(args.NewFullPath, provider);
                            }
                        }

                        return dic;
                    },
                    e);
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
            return _snapshotProviderByProjectPath.Values.Select(p => p.CurrentSnapshot).ToList();
        }
    }
}
