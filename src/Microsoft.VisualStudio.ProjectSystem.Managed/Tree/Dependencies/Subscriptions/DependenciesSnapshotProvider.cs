// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Build.Execution;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencySubscriptionsHostContract, typeof(ICrossTargetSubscriptionsHost))]
    [Export(typeof(IDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed partial class DependenciesSnapshotProvider : OnceInitializedOnceDisposedAsync, ICrossTargetSubscriptionsHost, IDependenciesSnapshotProvider
    {
        public const string DependencySubscriptionsHostContract = "DependencySubscriptionsHostContract";

        public event EventHandler<ProjectRenamedEventArgs>? SnapshotRenamed;
        public event EventHandler<SnapshotProviderUnloadingEventArgs>? SnapshotProviderUnloading;

        private readonly SemaphoreSlim _contextUpdateGate = new SemaphoreSlim(initialCount: 1);
        private readonly object _lock = new object();

        private readonly SnapshotUpdater _snapshot;

        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IAggregateCrossTargetProjectContextProvider> _contextProvider;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;
        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber> _dependencySubscribers;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> _subTreeProviders;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesSnapshotFilter> _snapshotFilters;

        /// <summary>
        /// Disposable items to be disposed when this provider is no longer in use.
        /// </summary>
        private readonly DisposableBag _disposables;

        /// <summary>
        /// Disposable items related to the current subscriptions. This collection may be replaced
        /// from time to time (e.g. when target frameworks change).
        /// </summary>
        private DisposableBag _subscriptions = new DisposableBag();

        /// <summary>
        /// Lazily populated set of subscribers. May be <see cref="ImmutableArray{T}.IsDefault" /> if <see cref="Subscribers"/>
        /// has not been called, though once initialized it will not revert to default state.
        /// </summary>
        private ImmutableArray<IDependencyCrossTargetSubscriber> _subscribers;

        private int _isInitialized;
        private bool _isDisposed;

        /// <summary>
        ///     Current <see cref="AggregateCrossTargetProjectContext"/>, which is an immutable map of
        ///     configured project to target framework.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     Updates of this field are serialized within a lock, but reads are free threaded as any
        ///     potential race can only be handled outside this class.
        /// </para>
        /// <para>
        ///     Value is null before initialization, and not null after.
        /// </para>
        /// </remarks>
        private AggregateCrossTargetProjectContext? _currentAggregateProjectContext;

        [ImportingConstructor]
        public DependenciesSnapshotProvider(
            IUnconfiguredProjectCommonServices commonServices,
            Lazy<IAggregateCrossTargetProjectContextProvider> contextProvider,
            IUnconfiguredProjectTasksService tasksService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
            ITargetFrameworkProvider targetFrameworkProvider,
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));
            Requires.NotNull(activeProjectConfigurationRefreshService, nameof(activeProjectConfigurationRefreshService));
            Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));
            Requires.NotNull(aggregateSnapshotProvider, nameof(aggregateSnapshotProvider));

            _commonServices = commonServices;
            _contextProvider = contextProvider;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeProjectConfigurationRefreshService = activeProjectConfigurationRefreshService;
            _targetFrameworkProvider = targetFrameworkProvider;
            _aggregateSnapshotProvider = aggregateSnapshotProvider;

            _dependencySubscribers = new OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber>(
                projectCapabilityCheckProvider: commonServices.Project);

            _snapshotFilters = new OrderPrecedenceImportCollection<IDependenciesSnapshotFilter>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            _subTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            _snapshot = new SnapshotUpdater(commonServices, tasksService.UnloadCancellationToken);

            _disposables = new DisposableBag { _snapshot, _contextUpdateGate };
        }

        /// <inheritdoc />
        public IDependenciesSnapshot CurrentSnapshot => _snapshot.Current;

        /// <inheritdoc />
        IReceivableSourceBlock<SnapshotChangedEventArgs> IDependenciesSnapshotProvider.SnapshotChangedSource => _snapshot.Source;

        private ImmutableArray<IDependencyCrossTargetSubscriber> Subscribers
        {
            get
            {
                if (_subscribers.IsDefault)
                {
                    lock (_lock)
                    {
                        if (_isDisposed)
                        {
                            throw new ObjectDisposedException(nameof(DependenciesSnapshotProvider));
                        }

                        if (_subscribers.IsDefault)
                        {
                            _subscribers = _dependencySubscribers.ToImmutableValueArray();

                            foreach (IDependencyCrossTargetSubscriber subscriber in _subscribers)
                            {
                                subscriber.DependenciesChanged += OnSubscriberDependenciesChanged;
                            }

                            _disposables.Add(new DisposableDelegate(
                                () =>
                                {
                                    foreach (IDependencyCrossTargetSubscriber subscriber in _subscribers)
                                    {
                                        subscriber.DependenciesChanged -= OnSubscriberDependenciesChanged;
                                    }
                                }));
                        }
                    }
                }

                return _subscribers;

                void OnSubscriberDependenciesChanged(object sender, DependencySubscriptionChangedEventArgs e)
                {
                    if (IsDisposing || IsDisposed)
                    {
                        return;
                    }

                    UpdateDependenciesSnapshot(e.Changes, e.Catalogs, e.TargetFrameworks, e.ActiveTarget, CancellationToken.None);
                }
            }
        }

#pragma warning disable RS0030 // symbol ProjectAutoLoad is banned
        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectFactoryCompleted)]
#pragma warning restore RS0030 // symbol ProjectAutoLoad is banned
        [AppliesTo(ProjectCapability.DependenciesTree)]
        public Task OnProjectFactoryCompletedAsync()
        {
            // The project factory is completing.

            // Subscribe to project data. Ensure the project doesn't unload during subscription.
            return _tasksService.LoadedProjectAsync(AddInitialSubscriptionsAsync);

            Task AddInitialSubscriptionsAsync()
            {
                lock (_lock)
                {
                    // This host object subscribes to configured project evaluation data for its own purposes.
                    SubscribeToConfiguredProjectEvaluation(
                        _activeConfiguredProjectSubscriptionService,
                        OnActiveConfiguredProjectEvaluatedAsync);

                    // Each of the host's subscribers are initialized.
                    return Task.WhenAll(
                        Subscribers.Select(
                            subscriber => subscriber.InitializeSubscriberAsync(this, _activeConfiguredProjectSubscriptionService)));
                }
            }

            async Task OnActiveConfiguredProjectEvaluatedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                if (IsDisposing || IsDisposed)
                {
                    return;
                }

                await EnsureInitializedAsync();

                await OnConfiguredProjectEvaluatedAsync(e);
            }
        }

        /// <summary>
        /// Workaround for CPS bug 375276 which causes double entry on InitializeAsync and exception
        /// "InvalidOperationException: The value factory has called for the value on the same instance".
        /// https://dev.azure.com/devdiv/DevDiv/_workitems/edit/375276
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (Interlocked.Exchange(ref _isInitialized, 1) == 0)
            {
                await InitializeAsync();
            }
        }

        /// <inheritdoc />
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await UpdateProjectContextAndSubscriptionsAsync();

            lock (_lock)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DependenciesSnapshotProvider));
                }

                IDisposable unregister = _aggregateSnapshotProvider.RegisterSnapshotProvider(this);

                _disposables.Add(unregister);

                _commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloadingAsync;
                _commonServices.Project.ProjectRenamed += OnUnconfiguredProjectRenamedAsync;

                foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
                {
                    provider.Value.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
                }

                _disposables.Add(
                    new DisposableDelegate(
                        () =>
                        {
                            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
                            _commonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

                            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
                            {
                                provider.Value.DependenciesChanged -= OnSubtreeProviderDependenciesChanged;
                            }
                        }));
            }

            return;

            Task OnUnconfiguredProjectUnloadingAsync(object sender, EventArgs args)
            {
                // If our project unloads, we have no more work to do. Notify listeners and clean everything up.

                SnapshotProviderUnloading?.Invoke(this, new SnapshotProviderUnloadingEventArgs(this));

                DisposeCore();

                return Task.CompletedTask;
            }

            Task OnUnconfiguredProjectRenamedAsync(object sender, ProjectRenamedEventArgs e)
            {
                SnapshotRenamed?.Invoke(this, e);

                return Task.CompletedTask;
            }

            void OnSubtreeProviderDependenciesChanged(object sender, DependenciesChangedEventArgs e)
            {
                if (IsDisposing || IsDisposed || !e.Changes.AnyChanges())
                {
                    return;
                }

                ITargetFramework targetFramework =
                    string.IsNullOrEmpty(e.TargetShortOrFullName) || TargetFramework.Any.Equals(e.TargetShortOrFullName)
                        ? TargetFramework.Any
                        : _targetFrameworkProvider.GetTargetFramework(e.TargetShortOrFullName) ?? TargetFramework.Any;

                ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes = ImmutableDictionary<ITargetFramework, IDependenciesChanges>
                    .Empty.Add(targetFramework, e.Changes);

                UpdateDependenciesSnapshot(changes, catalogs: null, targetFrameworks: default, activeTargetFramework: null, e.Token);
            }
        }

        /// <inheritdoc />
        protected override Task DisposeCoreAsync(bool initialized)
        {
            DisposeCore();

            return Task.CompletedTask;
        }

        private void DisposeCore()
        {
            lock (_lock)
            {
                _isDisposed = true;
                _subscriptions.Dispose();
                _disposables.Dispose();
            }
        }

        private void UpdateDependenciesSnapshot(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changesByTargetFramework,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework? activeTargetFramework,
            CancellationToken token)
        {
            IImmutableSet<string>? projectItemSpecs = GetProjectItemSpecsFromSnapshot();

            _snapshot.TryUpdate(
                previousSnapshot => DependenciesSnapshot.FromChanges(
                    _commonServices.Project.FullPath,
                    previousSnapshot,
                    changesByTargetFramework,
                    catalogs,
                    targetFrameworks,
                    activeTargetFramework,
                    _snapshotFilters.ToImmutableValueArray(),
                    _subTreeProviders.ToValueDictionary(p => p.ProviderType, StringComparers.DependencyProviderTypes),
                    projectItemSpecs),
                token);

            return;

            // Gets the set of items defined directly the project, and not included by imports.
            IImmutableSet<string>? GetProjectItemSpecsFromSnapshot()
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

        /// <inheritdoc />
        public async Task<AggregateCrossTargetProjectContext?> GetCurrentAggregateProjectContextAsync()
        {
            if (IsDisposing || IsDisposed)
            {
                return null;
            }

            await EnsureInitializedAsync();

            return _currentAggregateProjectContext;
        }

        /// <inheritdoc />
        public ConfiguredProject? GetConfiguredProject(ITargetFramework target)
        {
            return _currentAggregateProjectContext!.GetInnerConfiguredProject(target);
        }

        private Task OnConfiguredProjectEvaluatedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            if (HasTargetFrameworksChanged())
            {
                return UpdateProjectContextAndSubscriptionsAsync();
            }

            return Task.CompletedTask;

            bool HasTargetFrameworksChanged()
            {
                // remember actual property value and compare
                return e.Value.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectChangeDescription projectChange) &&
                       (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworkProperty) ||
                        projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty));
            }
        }

        /// <summary>
        /// Determines whether the current project context object is out of date based on the project's target frameworks.
        /// If so, a new one is created and subscriptions are updated accordingly.
        /// </summary>
        private async Task UpdateProjectContextAndSubscriptionsAsync()
        {
            // Prevent concurrent project context updates.
            await _contextUpdateGate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, async () =>
                {
                    AggregateCrossTargetProjectContext? newProjectContext = await TryUpdateCurrentAggregateProjectContextAsync();

                    if (newProjectContext != null)
                    {
                        _snapshot.TryUpdate(previousSnapshot => previousSnapshot.SetTargets(newProjectContext.TargetFrameworks, newProjectContext.ActiveTargetFramework));

                        // The context changed, so update a few things.
                        await _tasksService.LoadedProjectAsync(() =>
                        {
                            lock (_lock)
                            {
                                if (_isDisposed)
                                {
                                    throw new ObjectDisposedException(nameof(DependenciesSnapshotProvider));
                                }

                                // Dispose existing subscriptions.
                                _subscriptions.Dispose();
                                _subscriptions = new DisposableBag();

                                // Add subscriptions for the configured projects in the new project context.
                                AddSubscriptions(newProjectContext);
                            }

                            return Task.CompletedTask;
                        });
                    }
                });

            return;

            async Task<AggregateCrossTargetProjectContext?> TryUpdateCurrentAggregateProjectContextAsync()
            {
                AggregateCrossTargetProjectContext? previousContext = _currentAggregateProjectContext;

                // Check if we have already computed the project context.
                if (previousContext != null)
                {
                    // For non-cross targeting projects, we can use the current project context if the TargetFramework hasn't changed.
                    // For cross-targeting projects, we need to verify that the current project context matches latest frameworks targeted by the project.
                    // If not, we create a new one and dispose the current one.
                    ConfigurationGeneral projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();

                    if (!previousContext.IsCrossTargeting)
                    {
                        string newTargetFrameworkName = (string)await projectProperties.TargetFramework.GetValueAsync();
                        ITargetFramework? newTargetFramework = _targetFrameworkProvider.GetTargetFramework(newTargetFrameworkName);
                        if (previousContext.ActiveTargetFramework.Equals(newTargetFramework))
                        {
                            // No change
                            return null;
                        }
                    }
                    else
                    {
                        // Check if the current project context is up-to-date for the current active and known project configurations.
                        ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                        IImmutableSet<ProjectConfiguration> knownProjectConfigurations = await _commonServices.Project.Services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
                        if (knownProjectConfigurations.All(c => c.IsCrossTargeting()) &&
                            HasMatchingTargetFrameworks(activeProjectConfiguration, knownProjectConfigurations))
                        {
                            // No change
                            return null;
                        }
                    }
                }

                // Force refresh the CPS active project configuration (needs UI thread).
                await _commonServices.ThreadingService.SwitchToUIThread();
                await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync();

                // Create new project context.
                AggregateCrossTargetProjectContext newContext = await _contextProvider.Value.CreateProjectContextAsync();

                _currentAggregateProjectContext = newContext;

                return newContext;

                bool HasMatchingTargetFrameworks(
                    ProjectConfiguration activeProjectConfiguration,
                    IReadOnlyCollection<ProjectConfiguration> knownProjectConfigurations)
                {
                    Assumes.True(activeProjectConfiguration.IsCrossTargeting());

                    ITargetFramework? activeTargetFramework = _targetFrameworkProvider.GetTargetFramework(activeProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty]);

                    if (!previousContext.ActiveTargetFramework.Equals(activeTargetFramework))
                    {
                        // Active target framework is different.
                        return false;
                    }

                    var targetFrameworkMonikers = knownProjectConfigurations
                        .Select(c => c.Dimensions[ConfigurationGeneral.TargetFrameworkProperty])
                        .Distinct()
                        .ToList();

                    if (targetFrameworkMonikers.Count != previousContext.TargetFrameworks.Length)
                    {
                        // Different number of target frameworks.
                        return false;
                    }

                    foreach (string targetFrameworkMoniker in targetFrameworkMonikers)
                    {
                        ITargetFramework? targetFramework = _targetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);

                        if (targetFramework == null || !previousContext.TargetFrameworks.Contains(targetFramework))
                        {
                            // Differing TargetFramework
                            return false;
                        }
                    }

                    return true;
                }
            }

            void AddSubscriptions(AggregateCrossTargetProjectContext newProjectContext)
            {
                foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
                {
                    SubscribeToConfiguredProjectEvaluation(
                        configuredProject.Services.ProjectSubscription,
                        OnConfiguredProjectEvaluatedAsync);
                }

                foreach (IDependencyCrossTargetSubscriber subscriber in Subscribers)
                {
                    subscriber.AddSubscriptions(newProjectContext);
                }

                _subscriptions.Add(new DisposableDelegate(
                    () =>
                    {
                        foreach (IDependencyCrossTargetSubscriber subscriber in _subscribers)
                        {
                            subscriber.ReleaseSubscriptions();
                        }
                    }));
            }
        }

        private void SubscribeToConfiguredProjectEvaluation(
            IProjectSubscriptionService subscriptionService,
            Func<IProjectVersionedValue<IProjectSubscriptionUpdate>, Task> action)
        {
            _subscriptions.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                    action,
                    ruleNames: ConfigurationGeneral.SchemaName));
        }
    }
}
