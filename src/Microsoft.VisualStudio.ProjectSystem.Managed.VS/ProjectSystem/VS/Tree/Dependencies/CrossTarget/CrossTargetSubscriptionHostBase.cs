// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal abstract class CrossTargetSubscriptionHostBase : OnceInitializedOnceDisposedAsync, ICrossTargetSubscriptionsHost
    {
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked corretly by the IDisposeable analyzer
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
#pragma warning restore CA2213
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IAggregateCrossTargetProjectContextProvider> _contextProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly object _linksLock = new object();
        private readonly List<IDisposable> _evaluationSubscriptionLinks;
        private readonly object _initializationLock = new object();
        private bool _isInitialized;

        /// <summary>
        /// Current AggregateCrossTargetProjectContext - accesses to this field must be done with a lock.
        /// Note that at any given time, we can have only a single non-disposed aggregate project context.
        /// </summary>
        private AggregateCrossTargetProjectContext _currentAggregateProjectContext;

        public CrossTargetSubscriptionHostBase(IUnconfiguredProjectCommonServices commonServices,
                                   Lazy<IAggregateCrossTargetProjectContextProvider> contextProvider,
                                   [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
                                   IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
                                   IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
                                   ITargetFrameworkProvider targetFrameworkProvider)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));
            Requires.NotNull(activeProjectConfigurationRefreshService, nameof(activeProjectConfigurationRefreshService));
            Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));

            _commonServices = commonServices;
            _contextProvider = contextProvider;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeProjectConfigurationRefreshService = activeProjectConfigurationRefreshService;
            _targetFrameworkProvider = targetFrameworkProvider;
            _evaluationSubscriptionLinks = new List<IDisposable>();
        }

        protected abstract IEnumerable<Lazy<ICrossTargetSubscriber>> Subscribers { get; }

        public async Task<AggregateCrossTargetProjectContext> GetCurrentAggregateProjectContext()
        {
            if (IsDisposing || IsDisposed)
            {
                return null;
            }

            await EnsureInitialized().ConfigureAwait(false);

            return await ExecuteWithinLockAsync(() =>
            {
                return Task.FromResult(_currentAggregateProjectContext);
            }).ConfigureAwait(false);
        }

        public async Task<ConfiguredProject> GetConfiguredProject(ITargetFramework target)
        {
            return await ExecuteWithinLockAsync(() =>
            {
                return Task.FromResult(_currentAggregateProjectContext.GetInnerConfiguredProject(target));
            }).ConfigureAwait(false);
        }

        protected async Task AddInitialSubscriptionsAsync()
        {
            await _tasksService.LoadedProjectAsync(async () =>
            {
                SubscribeToConfiguredProject(_activeConfiguredProjectSubscriptionService,
                    new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnProjectChangedAsync(e, RuleHandlerType.Evaluation)));

                foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                {
                    await subscriber.Value.InitializeSubscriberAsync(this, _activeConfiguredProjectSubscriptionService)
                                          .ConfigureAwait(false);
                }
            });
        }

        protected virtual void OnAggregateContextChanged(AggregateCrossTargetProjectContext oldContext,
                                                         AggregateCrossTargetProjectContext newContext)
        {
            // by default do nothing
        }

        /// <summary>
        /// Workaround for CPS bug 375276 which causes double entry on InitializeAsync and exception
        /// "InvalidOperationException: The value factory has called for the value on the same instance".
        /// </summary>
        /// <returns></returns>
        private async Task EnsureInitialized()
        {
            bool shouldInitialize = false;
            lock (_initializationLock)
            {
                if (!_isInitialized)
                {
                    shouldInitialize = true;
                    _isInitialized = true;
                }
            }

            if (shouldInitialize)
            {
                await InitializeAsync().ConfigureAwait(false);
            }
        }

        protected async override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Don't initialize if we're unloading
            _tasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

            // Update project context and subscriptions.
            await UpdateProjectContextAndSubscriptionsAsync().ConfigureAwait(false);
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await EnsureInitialized().ConfigureAwait(false);

            await OnProjectChangedCoreAsync(e, handlerType).ConfigureAwait(false);
        }

        private async Task OnProjectChangedCoreAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            if (HasTargetFrameworksChanged(e))
            {
                await UpdateProjectContextAndSubscriptionsAsync().ConfigureAwait(false);
            }
        }

        private async Task UpdateProjectContextAndSubscriptionsAsync()
        {
            AggregateCrossTargetProjectContext previousProjectContext = await ExecuteWithinLockAsync(() =>
            {
                return Task.FromResult(_currentAggregateProjectContext);
            }).ConfigureAwait(false);

            AggregateCrossTargetProjectContext newProjectContext = await UpdateProjectContextAsync().ConfigureAwait(false);
            if (previousProjectContext != newProjectContext)
            {
                // Dispose existing subscriptions.
                DisposeAndClearSubscriptions();

                // Add subscriptions for the configured projects in the new project context.
                await AddSubscriptionsAsync(newProjectContext).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Ensures that <see cref="_currentAggregateProjectContext"/> is updated for the latest TargetFrameworks from the project properties
        /// and returns this value.
        /// </summary>
        private async Task<AggregateCrossTargetProjectContext> UpdateProjectContextAsync()
        {
            // Ensure that only single thread is attempting to create a project context.
            AggregateCrossTargetProjectContext previousContextToDispose = null;
            return await ExecuteWithinLockAsync(async () =>
            {
                // Check if we have already computed the project context.
                if (_currentAggregateProjectContext != null)
                {
                    // For non-cross targeting projects, we can use the current project context if the TargetFramework hasn't changed.
                    // For cross-targeting projects, we need to verify that the current project context matches latest frameworks targeted by the project.
                    // If not, we create a new one and dispose the current one.
                    ConfigurationGeneral projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);

                    if (!_currentAggregateProjectContext.IsCrossTargeting)
                    {
                        ITargetFramework newTargetFramework = _targetFrameworkProvider.GetTargetFramework((string)await projectProperties.TargetFramework.GetValueAsync().ConfigureAwait(false));
                        if (_currentAggregateProjectContext.ActiveProjectContext.TargetFramework.Equals(newTargetFramework))
                        {
                            return _currentAggregateProjectContext;
                        }
                    }
                    else
                    {
                        string targetFrameworks = (string)await projectProperties.TargetFrameworks.GetValueAsync().ConfigureAwait(false);

                        // Check if the current project context is up-to-date for the current active and known project configurations.
                        ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                        IImmutableSet<ProjectConfiguration> knownProjectConfigurations = await _commonServices.Project.Services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync().ConfigureAwait(false);
                        if (knownProjectConfigurations.All(c => c.IsCrossTargeting()) &&
                            _currentAggregateProjectContext.HasMatchingTargetFrameworks(activeProjectConfiguration, knownProjectConfigurations))
                        {
                            return _currentAggregateProjectContext;
                        }
                    }

                    previousContextToDispose = _currentAggregateProjectContext;
                }

                // Force refresh the CPS active project configuration (needs UI thread).
                await _commonServices.ThreadingService.SwitchToUIThread();
                await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync().ConfigureAwait(false);

                // Dispose the old project context, if one exists.
                if (previousContextToDispose != null)
                {
                    await DisposeAggregateProjectContextAsync(previousContextToDispose).ConfigureAwait(false);
                }

                // Create new project context.
                _currentAggregateProjectContext = await _contextProvider.Value.CreateProjectContextAsync().ConfigureAwait(false);

                OnAggregateContextChanged(previousContextToDispose, _currentAggregateProjectContext);

                return _currentAggregateProjectContext;
            }).ConfigureAwait(false);
        }

        private async Task DisposeAggregateProjectContextAsync(AggregateCrossTargetProjectContext projectContext)
        {
            await _contextProvider.Value.ReleaseProjectContextAsync(projectContext).ConfigureAwait(false);

            foreach (ITargetedProjectContext innerContext in projectContext.DisposedInnerProjectContexts)
            {
                foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                {
                    await subscriber.Value.OnContextReleasedAsync(innerContext).ConfigureAwait(false);
                }
            }
        }

        private async Task AddSubscriptionsAsync(AggregateCrossTargetProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            await _commonServices.ThreadingService.SwitchToUIThread();

            await _tasksService.LoadedProjectAsync(() =>
            {
                lock (_linksLock)
                {
                    foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
                    {
                        SubscribeToConfiguredProject(configuredProject.Services.ProjectSubscription,
                            new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                                              e => OnProjectChangedCoreAsync(e, RuleHandlerType.Evaluation)));
                    }

                    foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                    {
                        subscriber.Value.AddSubscriptionsAsync(newProjectContext);
                    }
                }

                return Task.CompletedTask;
            });
        }

        private void SubscribeToConfiguredProject(IProjectSubscriptionService subscriptionService,
            ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> actionBlock)
        {
            _evaluationSubscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    actionBlock,
                    ruleNames: new[] { ConfigurationGeneral.SchemaName },
                    suppressVersionOnlyUpdates: true));
        }

        private static bool HasTargetFrameworksChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // remember actual property value and compare
            return e.Value.ProjectChanges.TryGetValue(
                        ConfigurationGeneral.SchemaName, out IProjectChangeDescription projectChange) &&
                   (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworkProperty)
                    || projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty));
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                DisposeAndClearSubscriptions();

                await ExecuteWithinLockAsync(async () =>
                {
                    if (_currentAggregateProjectContext != null)
                    {
                        await _contextProvider.Value.ReleaseProjectContextAsync(_currentAggregateProjectContext).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }

        private void DisposeAndClearSubscriptions()
        {
            lock (_linksLock)
            {
                foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                {
                    subscriber.Value.ReleaseSubscriptionsAsync();
                }

                foreach (IDisposable link in _evaluationSubscriptionLinks)
                {
                    link.Dispose();
                }

                _evaluationSubscriptionLinks.Clear();
            }
        }

        private Task<T> ExecuteWithinLockAsync<T>(Func<Task<T>> task)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, task);
        }

        private Task ExecuteWithinLockAsync(Func<Task> task)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, task);
        }
    }
}
