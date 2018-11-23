// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Build.Execution;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencySubscriptionsHostContract, typeof(ICrossTargetSubscriptionsHost))]
    [Export(typeof(IDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencySubscriptionsHost : CrossTargetSubscriptionHostBase, IDependenciesSnapshotProvider
    {
        public const string DependencySubscriptionsHostContract = "DependencySubscriptionsHostContract";

        private readonly TimeSpan _dependenciesUpdateThrottleInterval = TimeSpan.FromMilliseconds(250);

        private readonly object _snapshotLock = new object();
        private readonly object _subscribersLock = new object();

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly ITaskDelayScheduler _dependenciesUpdateScheduler;

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber> _dependencySubscribers;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> _subTreeProviders;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesSnapshotFilter> _snapshotFilters;

        private IEnumerable<Lazy<ICrossTargetSubscriber>> _subscribers;
        private DependenciesSnapshot _currentSnapshot;

        [ImportingConstructor]
        public DependencySubscriptionsHost(
            IUnconfiguredProjectCommonServices commonServices,
            Lazy<IAggregateCrossTargetProjectContextProvider> contextProvider,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
            ITargetFrameworkProvider targetFrameworkProvider,
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(commonServices,
                   contextProvider,
                   tasksService,
                   activeConfiguredProjectSubscriptionService,
                   activeProjectConfigurationRefreshService,
                   targetFrameworkProvider)
        {
            _commonServices = commonServices;
            _targetFrameworkProvider = targetFrameworkProvider;
            _aggregateSnapshotProvider = aggregateSnapshotProvider;

            _dependencySubscribers = new OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber>(
                projectCapabilityCheckProvider: commonServices.Project);

            _snapshotFilters = new OrderPrecedenceImportCollection<IDependenciesSnapshotFilter>(
                projectCapabilityCheckProvider: commonServices.Project,
                orderingStyle: ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast);

            _subTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            _dependenciesUpdateScheduler = new TaskDelayScheduler(
                _dependenciesUpdateThrottleInterval,
                commonServices.ThreadingService,
                tasksService.UnloadCancellationToken);

            ProjectFilePath = commonServices.Project.FullPath;
        }

        #region IDependenciesSnapshotProvider

        public IDependenciesSnapshot CurrentSnapshot
        {
            get
            {
                if (_currentSnapshot == null)
                {
                    lock (_snapshotLock)
                    {
                        if (_currentSnapshot == null)
                        {
                            _currentSnapshot = DependenciesSnapshot.CreateEmpty(_commonServices.Project.FullPath);
                        }
                    }
                }

                return _currentSnapshot;
            }
        }

        public string ProjectFilePath { get; }

        public event EventHandler<ProjectRenamedEventArgs> SnapshotRenamed;
        public event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;
        public event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;

        #endregion

        protected override IEnumerable<Lazy<ICrossTargetSubscriber>> Subscribers
        {
            get
            {
                if (_subscribers == null)
                {
                    lock (_subscribersLock)
                    {
                        if (_subscribers == null)
                        {
                            foreach (Lazy<IDependencyCrossTargetSubscriber, IOrderPrecedenceMetadataView> subscriber in _dependencySubscribers)
                            {
                                subscriber.Value.DependenciesChanged += OnSubscriberDependenciesChanged;
                            }

                            _subscribers = _dependencySubscribers.Select(x => new Lazy<ICrossTargetSubscriber>(() => x.Value)).ToList();
                        }
                    }
                }

                return _subscribers;
            }
        }

        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DependenciesTree)]
        public Task OnProjectFactoryCompletedAsync()
        {
            return AddInitialSubscriptionsAsync();
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await base.InitializeCoreAsync(cancellationToken);

            _commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloadingAsync;
            _commonServices.Project.ProjectRenamed += OnUnconfiguredProjectRenamedAsync;

            _aggregateSnapshotProvider.RegisterSnapshotProvider(this);

            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
            {
                provider.Value.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
            _commonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

            _dependenciesUpdateScheduler.Dispose();

            await base.DisposeCoreAsync(initialized);
        }

        private Task OnUnconfiguredProjectUnloadingAsync(object sender, EventArgs args)
        {
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
            _commonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

            SnapshotProviderUnloading?.Invoke(this, new SnapshotProviderUnloadingEventArgs(this));

            foreach (Lazy<IDependencyCrossTargetSubscriber, IOrderPrecedenceMetadataView> subscriber in _dependencySubscribers)
            {
                subscriber.Value.DependenciesChanged -= OnSubscriberDependenciesChanged;
            }

            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
            {
                provider.Value.DependenciesChanged -= OnSubtreeProviderDependenciesChanged;
            }

            return Task.CompletedTask;
        }

        private Task OnUnconfiguredProjectRenamedAsync(object sender, ProjectRenamedEventArgs e)
        {
            SnapshotRenamed?.Invoke(this, e);

            return Task.CompletedTask;
        }

        protected override void OnAggregateContextChanged(
            AggregateCrossTargetProjectContext oldContext,
            AggregateCrossTargetProjectContext newContext)
        {
            if (oldContext == null)
            {
                // all new rules will be sent to new context , we don't need to clean up anything
                return;
            }

            var targetsToClean = new HashSet<ITargetFramework>();

            IEnumerable<ITargetFramework> oldTargets = oldContext.InnerProjectContexts
                .Select(x => x.TargetFramework)
                .Where(x => x != null);

            if (newContext == null)
            {
                targetsToClean.AddRange(oldTargets);
            }
            else
            {
                IEnumerable<ITargetFramework> newTargets = newContext.InnerProjectContexts
                    .Select(x => x.TargetFramework)
                    .Where(x => x != null);

                targetsToClean.AddRange(oldTargets.Except(newTargets));
            }

            if (targetsToClean.Count == 0)
            {
                return;
            }

            lock (_snapshotLock)
            {
                _currentSnapshot = _currentSnapshot.RemoveTargets(targetsToClean);
            }

            ScheduleDependenciesUpdate();
        }

        private void OnSubscriberDependenciesChanged(object sender, DependencySubscriptionChangedEventArgs e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            if (!e.AnyChanges)
            {
                return;
            }

            UpdateDependenciesSnapshot(e.Changes, e.Catalogs, e.ActiveTarget, CancellationToken.None);
        }

        private void OnSubtreeProviderDependenciesChanged(object sender, DependenciesChangedEventArgs e)
        {
            if (IsDisposing || IsDisposed || !e.Changes.AnyChanges())
            {
                return;
            }

            ITargetFramework targetFramework =
                string.IsNullOrEmpty(e.TargetShortOrFullName) || TargetFramework.Any.Equals(e.TargetShortOrFullName)
                    ? TargetFramework.Any
                    : _targetFrameworkProvider.GetTargetFramework(e.TargetShortOrFullName) ?? TargetFramework.Any;

            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes = ImmutableDictionary<ITargetFramework, IDependenciesChanges>.Empty.Add(targetFramework, e.Changes);

            UpdateDependenciesSnapshot(changes, catalogs: null, activeTargetFramework: null, e.Token);
        }

        private void UpdateDependenciesSnapshot(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework,
            CancellationToken token)
        {
            IImmutableSet<string> projectItemSpecs = GetProjectItemSpecsFromSnapshot();

            bool anyChanges = false;
            
            // Note: we are updating existing snapshot, not receiving a complete new one. Thus we must
            // ensure incremental updates are done in the correct order. This lock ensures that here.

            lock (_snapshotLock)
            {
                var updatedSnapshot = DependenciesSnapshot.FromChanges(
                    _commonServices.Project.FullPath,
                    _currentSnapshot,
                    changes,
                    catalogs,
                    activeTargetFramework,
                    _snapshotFilters.ToImmutableValueArray(),
                    _subTreeProviders.ToValueDictionary(p => p.ProviderType),
                    projectItemSpecs);

                if (!ReferenceEquals(_currentSnapshot, updatedSnapshot))
                {
                    _currentSnapshot = updatedSnapshot;
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                // avoid unnecessary tree updates
                ScheduleDependenciesUpdate(token);
            }

            return;

            IImmutableSet<string> GetProjectItemSpecsFromSnapshot()
            {
                // We don't have catalog snapshot, we're likely updating because one of our project 
                // dependencies changed. Just return 'no data'
                if (catalogs == null)
                {
                    return null;
                }

                ImmutableHashSet<string>.Builder itemSpecs = ImmutableHashSet.CreateBuilder(StringComparer.OrdinalIgnoreCase);

                foreach (ProjectItemInstance item in catalogs.Project.ProjectInstance.Items)
                {
                    if (item.IsImported())
                    {
                        continue;
                    }

                    // Returns unescaped evaluated include
                    string itemSpec = item.EvaluatedInclude;
                    if (itemSpec.Length != 0)
                    {
                        itemSpecs.Add(itemSpec);
                    }
                }

                return itemSpecs.ToImmutable();
            }
        }

        private void ScheduleDependenciesUpdate(CancellationToken token = default)
        {
            _dependenciesUpdateScheduler.ScheduleAsyncTask(ct =>
            {
                if (ct.IsCancellationRequested || IsDisposing || IsDisposed)
                {
                    return Task.FromCanceled(ct);
                }

                IDependenciesSnapshot snapshot = _currentSnapshot;

                if (snapshot != null)
                {
                    SnapshotChanged?.Invoke(this, new SnapshotChangedEventArgs(snapshot, ct));
                }

                return Task.CompletedTask;
            }, token);
        }
    }
}
