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

        [ImportingConstructor]
        public DependencySubscriptionsHost(
            IUnconfiguredProjectCommonServices commonServices,
            Lazy<IAggregateCrossTargetProjectContextProvider> contextProvider,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
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
            CommonServices = commonServices;
            DependencySubscribers = new OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber>(
                projectCapabilityCheckProvider: commonServices.Project);

            SnapshotFilters = new OrderPrecedenceImportCollection<IDependenciesSnapshotFilter>(
                projectCapabilityCheckProvider: commonServices.Project,
                orderingStyle: ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast);

            SubTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            DependenciesUpdateScheduler = new TaskDelayScheduler(
                _dependenciesUpdateThrottleInterval,
                commonServices.ThreadingService,
                tasksService.UnloadCancellationToken);

            TargetFrameworkProvider = targetFrameworkProvider;
            AggregateSnapshotProvider = aggregateSnapshotProvider;
            ProjectFilePath = CommonServices.Project.FullPath;
        }

        private readonly object _snapshotLock = new object();
        private DependenciesSnapshot _currentSnapshot;

        #region IDependenciesSnapshotProvider

        public IDependenciesSnapshot CurrentSnapshot
        {
            get
            {
                lock (_snapshotLock)
                {
                    if (_currentSnapshot == null)
                    {
                        _currentSnapshot = DependenciesSnapshot.CreateEmpty(CommonServices.Project.FullPath);
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

        private IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }
        private ITargetFrameworkProvider TargetFrameworkProvider { get; }
        private IUnconfiguredProjectCommonServices CommonServices { get; }
        private ITaskDelayScheduler DependenciesUpdateScheduler { get; }

        [ImportMany]
        private OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber> DependencySubscribers { get; }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> SubTreeProviders { get; }

        [ImportMany]
        private OrderPrecedenceImportCollection<IDependenciesSnapshotFilter> SnapshotFilters { get; }
        private object _subscribersLock = new object();
        private IEnumerable<Lazy<ICrossTargetSubscriber>> _subscribers;
        protected override IEnumerable<Lazy<ICrossTargetSubscriber>> Subscribers
        {
            get
            {
                lock (_subscribersLock)
                {
                    if (_subscribers == null)
                    {
                        foreach (var subscriber in DependencySubscribers)
                        {
                            subscriber.Value.DependenciesChanged += OnSubscriberDependenciesChanged;
                        }

                        _subscribers = DependencySubscribers.Select(x => new Lazy<ICrossTargetSubscriber>(() => x.Value)).ToList();
                    }
                    return _subscribers;
                }
            }
        }

        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DependenciesTree)]
        private Task OnProjectFactoryCompletedAsync()
        {
            return AddInitialSubscriptionsAsync();
        }

        protected async override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            CommonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;
            CommonServices.Project.ProjectRenamed += OnUnconfiguredProjectRenamed;

            AggregateSnapshotProvider.RegisterSnapshotProvider(this);

            foreach (var provider in SubTreeProviders)
            {
                provider.Value.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            CommonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;
            CommonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamed;

            await base.DisposeCoreAsync(initialized).ConfigureAwait(false);
        }

        private Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
        {
            CommonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;
            CommonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamed;

            SnapshotProviderUnloading?.Invoke(this, new SnapshotProviderUnloadingEventArgs(this));

            foreach (var subscriber in DependencySubscribers)
            {
                subscriber.Value.DependenciesChanged -= OnSubscriberDependenciesChanged;
            }

            foreach (var provider in SubTreeProviders)
            {
                provider.Value.DependenciesChanged -= OnSubtreeProviderDependenciesChanged;
            }

            return Task.CompletedTask;
        }

        private Task OnUnconfiguredProjectRenamed(object sender, ProjectRenamedEventArgs e)
        {
            SnapshotRenamed?.Invoke(this, e);

            return Task.CompletedTask;
        }

        protected override void OnAggregateContextChanged(AggregateCrossTargetProjectContext oldContext,
                                                          AggregateCrossTargetProjectContext newContext)
        {
            if (oldContext == null)
            {
                // all new rules will be sent to new context , we don't need to clean up anything
                return;
            }

            var targetsToClean = new HashSet<ITargetFramework>();
            var oldTargets = oldContext.InnerProjectContexts.Select(x => x.TargetFramework)
                                                            .Where(x => x != null).ToList();
            if (newContext == null)
            {
                targetsToClean.AddRange(oldTargets);
            }
            else
            {
                var newTargets = newContext.InnerProjectContexts.Select(x => x.TargetFramework)
                                                                .Where(x => x != null).ToList();
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

            if (!e.Context.AnyChanges)
            {
                return;
            }

            UpdateDependenciesSnapshotAsync(e.Context.Changes, e.Context.Catalogs, e.Context.ActiveTarget);
        }

        private void OnSubtreeProviderDependenciesChanged(object sender, DependenciesChangedEventArgs e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            if (!e.Changes.AnyChanges())
            {
                return;
            }

            ITargetFramework targetFramework =
                string.IsNullOrEmpty(e.TargetShortOrFullName) || TargetFramework.Any.Equals(e.TargetShortOrFullName)
                    ? TargetFramework.Any
                    : TargetFrameworkProvider.GetTargetFramework(e.TargetShortOrFullName);
            if (targetFramework == null)
            {
                targetFramework = TargetFramework.Any;
            }

            var changes = ImmutableDictionary<ITargetFramework, IDependenciesChanges>.Empty.Add(targetFramework, e.Changes);

            UpdateDependenciesSnapshotAsync(changes, e.Catalogs, activeTargetFramework: null);
        }

        private void OnDependenciesSnapshotChanged(IDependenciesSnapshot snapshot)
        {
            SnapshotChanged?.Invoke(this, new SnapshotChangedEventArgs(snapshot));
        }

        private void UpdateDependenciesSnapshotAsync(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework)
        {
            DependenciesSnapshot newSnapshot;
            bool anyChanges = false;

            HashSet<string> projectItemSpecs = GetProjectItemSpecsFromSnapshot(catalogs);

            // Note: we are updating existing snapshot, not receivig a complete new one. Thus we must
            // ensure incremental updates are done in the correct order. This lock ensures that here.
            lock (_snapshotLock)
            {
                newSnapshot = DependenciesSnapshot.FromChanges(
                    CommonServices.Project.FullPath,
                    _currentSnapshot,
                    changes,
                    catalogs,
                    activeTargetFramework,
                    SnapshotFilters.Select(x => x.Value),
                    SubTreeProviders.Select(x => x.Value),
                    projectItemSpecs,
                    out anyChanges);
                _currentSnapshot = newSnapshot;
            }

            if (anyChanges)
            {
                // avoid unnecessary tree updates
                ScheduleDependenciesUpdate();
            }
        }

        private HashSet<string> GetProjectItemSpecsFromSnapshot(IProjectCatalogSnapshot catalogs)
        {
            // We don't have catalog snapshot, we're likely updating because one of our project 
            // dependencies changed. Just return 'no data'
            if (catalogs == null)
                return null;

            var projectItemSpecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ProjectItemInstance item in catalogs.Project.ProjectInstance.Items)
            {
                if (item.IsImported())
                    continue;

                // Returns unescaped evaluated include
                string itemSpec = item.EvaluatedInclude;
                if (itemSpec.Length > 0)
                    projectItemSpecs.Add(itemSpec);
            }

            return projectItemSpecs;
        }

        private void ScheduleDependenciesUpdate()
        {
            DependenciesUpdateScheduler.ScheduleAsyncTask((token) =>
            {
                if (token.IsCancellationRequested || IsDisposing || IsDisposed)
                {
                    return Task.FromCanceled(token);
                }

                var snapshot = CurrentSnapshot;
                if (snapshot == null)
                {
                    return Task.CompletedTask;
                }

                OnDependenciesSnapshotChanged(snapshot);

                return Task.CompletedTask;
            });
        }
    }
}