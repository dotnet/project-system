// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    /// <summary>
    /// Provides immutable dependencies snapshot for a given project.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    [Export(typeof(DependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed partial class DependenciesSnapshotProvider : OnceInitializedOnceDisposedAsync
    {
        private readonly SemaphoreSlim _contextUpdateGate = new(initialCount: 1);
        private readonly object _lock = new();

        private readonly SnapshotUpdater _snapshot;
        private readonly ContextTracker _context;

        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly IUnconfiguredProjectTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IDependencyTreeTelemetryService _dependencyTreeTelemetryService;

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber> _dependencySubscribers;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> _subTreeProviders;

        /// <summary>
        /// Disposable items to be disposed when this provider is no longer in use.
        /// </summary>
        private readonly DisposableBag _disposables;

        /// <summary>
        /// Disposable items related to the current subscriptions. This collection may be replaced
        /// from time to time (e.g. when target frameworks change).
        /// </summary>
        private DisposableBag _subscriptions = new();

        /// <summary>
        /// Lazily populated set of subscribers. May be <see cref="ImmutableArray{T}.IsDefault" /> if <see cref="Subscribers"/>
        /// has not been called, though once initialized it will not revert to default state.
        /// </summary>
        private ImmutableArray<IDependencyCrossTargetSubscriber> _subscribers;

        private bool _isDisposed;

        [ImportingConstructor]
        public DependenciesSnapshotProvider(
            IUnconfiguredProjectCommonServices commonServices,
            Lazy<AggregateCrossTargetProjectContextProvider> contextProvider,
            IUnconfiguredProjectTasksService tasksService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
            ITargetFrameworkProvider targetFrameworkProvider,
            IDependencyTreeTelemetryService dependencyTreeTelemetryService)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _commonServices = commonServices;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _targetFrameworkProvider = targetFrameworkProvider;
            _dependencyTreeTelemetryService = dependencyTreeTelemetryService;

            _dependencySubscribers = new OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber>(
                projectCapabilityCheckProvider: commonServices.Project);

            _subTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            _context = new ContextTracker(targetFrameworkProvider, commonServices, contextProvider, activeProjectConfigurationRefreshService);

            _snapshot = new SnapshotUpdater(commonServices.ThreadingService, tasksService.UnloadCancellationToken);

            _disposables = new DisposableBag { _snapshot, _contextUpdateGate };
        }

        /// <summary>
        /// Gets the current immutable dependencies snapshot for the project.
        /// </summary>
        /// <remarks>
        /// Never null.
        /// </remarks>
        public DependenciesSnapshot CurrentSnapshot => _snapshot.Current;

        /// <summary>
        /// Dataflow to monitor the project snapshot changes.
        /// </summary>
        public IReceivableSourceBlock<SnapshotChangedEventArgs> SnapshotChangedSource => _snapshot.Source;

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

                    UpdateDependenciesSnapshot(e.ChangedTargetFramework, e.Changes, e.Catalogs, e.TargetFrameworks, e.ActiveTarget, CancellationToken.None);
                }
            }
        }

        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DependenciesTree)]
        public Task OnProjectFactoryCompletedAsync()
        {
            // The project factory is completing.

            // Subscribe to project data. Ensure the project doesn't unload during subscription.
            return _tasksService.LoadedProjectAsync(AddInitialSubscriptionsAsync);

            Task AddInitialSubscriptionsAsync()
            {
                // This host object subscribes to configured project evaluation data for its own purposes.
                SubscribeToConfiguredProjectEvaluation(
                    _activeConfiguredProjectSubscriptionService,
                    OnActiveConfiguredProjectEvaluatedAsync);

                // Each of the host's subscribers are initialized.
                return Task.WhenAll(
                    Subscribers.Select(
                        subscriber => subscriber.InitializeSubscriberAsync(this)));
            }

            async Task OnActiveConfiguredProjectEvaluatedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
            {
                if (IsDisposing || IsDisposed)
                {
                    return;
                }

                await OnConfiguredProjectEvaluatedAsync(e);
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await UpdateProjectContextAndSubscriptionsAsync();

            lock (_lock)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DependenciesSnapshotProvider));
                }

                foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
                {
                    provider.Value.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
                }

                _disposables.Add(
                    new DisposableDelegate(
                        () =>
                        {
                            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
                            {
                                provider.Value.DependenciesChanged -= OnSubtreeProviderDependenciesChanged;
                            }
                        }));
            }

            return;

            void OnSubtreeProviderDependenciesChanged(object? sender, DependenciesChangedEventArgs e)
            {
                if (IsDisposing || IsDisposed || !e.Changes.AnyChanges())
                {
                    return;
                }

                TargetFramework targetFramework =
                    Strings.IsNullOrEmpty(e.TargetShortOrFullName) || TargetFramework.Any.Equals(e.TargetShortOrFullName)
                        ? TargetFramework.Any
                        : _targetFrameworkProvider.GetTargetFramework(e.TargetShortOrFullName) ?? TargetFramework.Any;

                UpdateDependenciesSnapshot(targetFramework, e.Changes, catalogs: null, targetFrameworks: default, activeTargetFramework: null, e.Token);
            }
        }

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
            TargetFramework changedTargetFramework,
            IDependenciesChanges? changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<TargetFramework> targetFrameworks,
            TargetFramework? activeTargetFramework,
            CancellationToken token)
        {
            Assumes.NotNull(_commonServices.Project.FullPath);

            DependenciesSnapshot? updatedSnapshot = _snapshot.TryUpdate(
                previousSnapshot => DependenciesSnapshot.FromChanges(
                    previousSnapshot,
                    changedTargetFramework,
                    changes,
                    catalogs,
                    targetFrameworks,
                    activeTargetFramework),
                token);

            if (updatedSnapshot is not null)
            {
                _dependencyTreeTelemetryService.ObserveSnapshot(updatedSnapshot);
            }
        }

        public async Task<AggregateCrossTargetProjectContext?> GetCurrentAggregateProjectContextAsync(CancellationToken cancellationToken)
        {
            if (IsDisposing || IsDisposed)
            {
                return null;
            }

            await InitializeAsync(cancellationToken);

            return _context.Current;
        }

        public ConfiguredProject? GetConfiguredProject(TargetFramework target)
        {
            return _context.Current!.GetInnerConfiguredProject(target);
        }

        private Task OnConfiguredProjectEvaluatedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            // If no context exists yet, create one.
            if (HasTargetFrameworksChanged() || _context.Current is null)
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
        private Task UpdateProjectContextAndSubscriptionsAsync()
        {
            // Prevent concurrent project context updates.
            return _contextUpdateGate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, async () =>
            {
                AggregateCrossTargetProjectContext? newProjectContext = await _context.TryUpdateCurrentAggregateProjectContextAsync();

                if (newProjectContext is not null)
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

            void AddSubscriptions(AggregateCrossTargetProjectContext newProjectContext)
            {
                foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
                {
                    Assumes.Present(configuredProject.Services.ProjectSubscription);

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
                    _commonServices.Project,
                    ruleNames: ConfigurationGeneral.SchemaName));
        }
    }

    internal sealed class SnapshotChangedEventArgs : EventArgs
    {
        public SnapshotChangedEventArgs(DependenciesSnapshot snapshot, CancellationToken token)
        {
            Requires.NotNull(snapshot, nameof(snapshot));

            Snapshot = snapshot;
            Token = token;
        }

        public DependenciesSnapshot Snapshot { get; }
        public CancellationToken Token { get; }
    }
}
