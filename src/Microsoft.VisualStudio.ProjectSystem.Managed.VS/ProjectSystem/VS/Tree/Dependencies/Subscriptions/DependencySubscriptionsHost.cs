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
            CommonServices = commonServices;
            TargetFrameworkProvider = targetFrameworkProvider;
            AggregateSnapshotProvider = aggregateSnapshotProvider;

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

            ProjectFilePath = commonServices.Project.FullPath;
        }

        private readonly object _snapshotLock = new object();
        private DependenciesSnapshot _currentSnapshot;

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
                            _currentSnapshot = DependenciesSnapshot.CreateEmpty(CommonServices.Project.FullPath);
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
        private readonly object _subscribersLock = new object();
        private IEnumerable<Lazy<ICrossTargetSubscriber>> _subscribers;
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
                            foreach (Lazy<IDependencyCrossTargetSubscriber, IOrderPrecedenceMetadataView> subscriber in DependencySubscribers)
                            {
                                subscriber.Value.DependenciesChanged += OnSubscriberDependenciesChanged;
                            }

                            _subscribers = DependencySubscribers.Select(x => new Lazy<ICrossTargetSubscriber>(() => x.Value)).ToList();
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

            CommonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloadingAsync;
            CommonServices.Project.ProjectRenamed += OnUnconfiguredProjectRenamedAsync;

            AggregateSnapshotProvider.RegisterSnapshotProvider(this);

            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in SubTreeProviders)
            {
                provider.Value.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            CommonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
            CommonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

            await base.DisposeCoreAsync(initialized);
        }

        private Task OnUnconfiguredProjectUnloadingAsync(object sender, EventArgs args)
        {
            CommonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
            CommonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

            SnapshotProviderUnloading?.Invoke(this, new SnapshotProviderUnloadingEventArgs(this));

            foreach (Lazy<IDependencyCrossTargetSubscriber, IOrderPrecedenceMetadataView> subscriber in DependencySubscribers)
            {
                subscriber.Value.DependenciesChanged -= OnSubscriberDependenciesChanged;
            }

            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in SubTreeProviders)
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

            if (!e.Context.AnyChanges)
            {
                return;
            }

            UpdateDependenciesSnapshotAsync(e.Context.Changes, e.Context.Catalogs, e.Context.ActiveTarget);
        }

        private void OnSubtreeProviderDependenciesChanged(object sender, DependenciesChangedEventArgs e)
        {
            if (IsDisposing || IsDisposed || !e.Changes.AnyChanges() || e.Token.IsCancellationRequested)
            {
                return;
            }

            ITargetFramework targetFramework =
                string.IsNullOrEmpty(e.TargetShortOrFullName) || TargetFramework.Any.Equals(e.TargetShortOrFullName)
                    ? TargetFramework.Any
                    : TargetFrameworkProvider.GetTargetFramework(e.TargetShortOrFullName) ?? TargetFramework.Any;

            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes = ImmutableDictionary<ITargetFramework, IDependenciesChanges>.Empty.Add(targetFramework, e.Changes);

            UpdateDependenciesSnapshotAsync(changes, catalogs: null, activeTargetFramework: null, e.Token);
        }

        private void UpdateDependenciesSnapshotAsync(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework,
            CancellationToken token = default)
        {
            bool anyChanges;

            IImmutableSet<string> projectItemSpecs = GetProjectItemSpecsFromSnapshot(catalogs);

            // Note: we are updating existing snapshot, not receiving a complete new one. Thus we must
            // ensure incremental updates are done in the correct order. This lock ensures that here.
            lock (_snapshotLock)
            {
                _currentSnapshot = DependenciesSnapshot.FromChanges(
                    CommonServices.Project.FullPath,
                    _currentSnapshot,
                    changes,
                    catalogs,
                    activeTargetFramework,
                    SnapshotFilters.Select(x => x.Value).ToList(),
                    SubTreeProviders.Select(x => x.Value).ToList(),
                    projectItemSpecs,
                    out anyChanges);
            }

            if (anyChanges)
            {
                // avoid unnecessary tree updates
                ScheduleDependenciesUpdate(token);
            }
        }

        private static IImmutableSet<string> GetProjectItemSpecsFromSnapshot(IProjectCatalogSnapshot catalogs)
        {
            // We don't have catalog snapshot, we're likely updating because one of our project 
            // dependencies changed. Just return 'no data'
            if (catalogs == null)
                return null;

            ImmutableHashSet<string>.Builder projectItemSpecs = ImmutableHashSet.CreateBuilder(StringComparer.OrdinalIgnoreCase);

            foreach (ProjectItemInstance item in catalogs.Project.ProjectInstance.Items)
            {
                if (item.IsImported())
                    continue;

                // Returns unescaped evaluated include
                string itemSpec = item.EvaluatedInclude;
                if (itemSpec.Length > 0)
                    projectItemSpecs.Add(itemSpec);
            }

            return projectItemSpecs.ToImmutable();
        }

        private void ScheduleDependenciesUpdate(CancellationToken token = default)
        {
            DependenciesUpdateScheduler.ScheduleAsyncTask(ct =>
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
