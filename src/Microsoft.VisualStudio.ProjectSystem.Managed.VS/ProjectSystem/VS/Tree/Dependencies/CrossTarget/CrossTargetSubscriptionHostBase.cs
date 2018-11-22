// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal abstract class CrossTargetSubscriptionHostBase : OnceInitializedOnceDisposedAsync, ICrossTargetSubscriptionsHost
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IAggregateCrossTargetProjectContextProvider> _contextProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly object _linksLock = new object();
        private readonly List<IDisposable> _evaluationSubscriptionLinks = new List<IDisposable>();

        private int _isInitialized;

        /// <summary>
        /// Current AggregateCrossTargetProjectContext - accesses to this field must be done with a lock.
        /// Note that at any given time, we can have only a single non-disposed aggregate project context.
        /// </summary>
        private AggregateCrossTargetProjectContext _currentAggregateProjectContext;

        protected CrossTargetSubscriptionHostBase(
            IUnconfiguredProjectCommonServices commonServices,
            Lazy<IAggregateCrossTargetProjectContextProvider> contextProvider,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
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
        }

        protected abstract IEnumerable<Lazy<ICrossTargetSubscriber>> Subscribers { get; }

        public async Task<AggregateCrossTargetProjectContext> GetCurrentAggregateProjectContext()
        {
            if (IsDisposing || IsDisposed)
            {
                return null;
            }

            await EnsureInitialized();

            return await ExecuteWithinLockAsync(() => _currentAggregateProjectContext);
        }

        public Task<ConfiguredProject> GetConfiguredProject(ITargetFramework target)
        {
            return ExecuteWithinLockAsync(() => _currentAggregateProjectContext.GetInnerConfiguredProject(target));
        }

        protected async Task AddInitialSubscriptionsAsync()
        {
            await _tasksService.LoadedProjectAsync(() =>
            {
                SubscribeToConfiguredProject(
                    _activeConfiguredProjectSubscriptionService,
                    e => OnProjectChangedAsync(e)); // evaluation

                foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                {
                    subscriber.Value.InitializeSubscriber(this, _activeConfiguredProjectSubscriptionService);
                }

                return Task.CompletedTask;
            });
        }

        protected virtual void OnAggregateContextChanged(
            AggregateCrossTargetProjectContext oldContext,
            AggregateCrossTargetProjectContext newContext)
        {
            // by default do nothing
        }

        /// <summary>
        /// Workaround for CPS bug 375276 which causes double entry on InitializeAsync and exception
        /// "InvalidOperationException: The value factory has called for the value on the same instance".
        /// https://dev.azure.com/devdiv/DevDiv/_workitems/edit/375276
        /// </summary>
        private async Task EnsureInitialized()
        {
            if (Interlocked.Exchange(ref _isInitialized, 1) == 0)
            {
                await InitializeAsync();
            }
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return UpdateProjectContextAndSubscriptionsAsync();
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await EnsureInitialized();

            await OnProjectChangedCoreAsync(e);
        }

        private Task OnProjectChangedCoreAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            if (HasTargetFrameworksChanged(e))
            {
                return UpdateProjectContextAndSubscriptionsAsync();
            }

            return Task.CompletedTask;
        }

        private async Task UpdateProjectContextAndSubscriptionsAsync()
        {
            AggregateCrossTargetProjectContext previousProjectContext = await ExecuteWithinLockAsync(() => _currentAggregateProjectContext);

            AggregateCrossTargetProjectContext newProjectContext = await UpdateProjectContextAsync();

            if (previousProjectContext != newProjectContext)
            {
                // Dispose existing subscriptions.
                DisposeAndClearSubscriptions();

                // Add subscriptions for the configured projects in the new project context.
                await AddSubscriptionsAsync(newProjectContext);
            }
        }

        /// <summary>
        /// Ensures that <see cref="_currentAggregateProjectContext"/> is updated for the latest TargetFrameworks from the project properties
        /// and returns this value.
        /// </summary>
        private Task<AggregateCrossTargetProjectContext> UpdateProjectContextAsync()
        {
            // Ensure that only single thread is attempting to create a project context.
            return ExecuteWithinLockAsync(async () =>
            {
                AggregateCrossTargetProjectContext previousContextToDispose = null;

                // Check if we have already computed the project context.
                if (_currentAggregateProjectContext != null)
                {
                    // For non-cross targeting projects, we can use the current project context if the TargetFramework hasn't changed.
                    // For cross-targeting projects, we need to verify that the current project context matches latest frameworks targeted by the project.
                    // If not, we create a new one and dispose the current one.
                    ConfigurationGeneral projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();

                    if (!_currentAggregateProjectContext.IsCrossTargeting)
                    {
                        ITargetFramework newTargetFramework = _targetFrameworkProvider.GetTargetFramework((string)await projectProperties.TargetFramework.GetValueAsync());
                        if (_currentAggregateProjectContext.ActiveTargetFramework.Equals(newTargetFramework))
                        {
                            return _currentAggregateProjectContext;
                        }
                    }
                    else
                    {
                        // Check if the current project context is up-to-date for the current active and known project configurations.
                        ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                        IImmutableSet<ProjectConfiguration> knownProjectConfigurations = await _commonServices.Project.Services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
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
                await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync();

                // Dispose the old project context, if one exists.
                if (previousContextToDispose != null)
                {
                    _contextProvider.Value.ReleaseProjectContext(previousContextToDispose);
                }

                // Create new project context.
                _currentAggregateProjectContext = await _contextProvider.Value.CreateProjectContextAsync();

                OnAggregateContextChanged(previousContextToDispose, _currentAggregateProjectContext);

                return _currentAggregateProjectContext;
            });
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
                        SubscribeToConfiguredProject(
                            configuredProject.Services.ProjectSubscription,
                            e => OnProjectChangedCoreAsync(e)); // evaluation
                    }

                    foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                    {
                        subscriber.Value.AddSubscriptions(newProjectContext);
                    }
                }

                return Task.CompletedTask;
            });
        }

        private void SubscribeToConfiguredProject(
            IProjectSubscriptionService subscriptionService,
            Func<IProjectVersionedValue<IProjectSubscriptionUpdate>, Task> action)
        {
            _evaluationSubscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                    action,
                    ruleNames: ConfigurationGeneral.SchemaName));
        }

        private static bool HasTargetFrameworksChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // remember actual property value and compare
            return e.Value.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectChangeDescription projectChange) &&
                   (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworkProperty) ||
                    projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty));
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            _gate.Dispose();

            if (initialized)
            {
                DisposeAndClearSubscriptions();

                await ExecuteWithinLockAsync(() =>
                {
                    if (_currentAggregateProjectContext != null)
                    {
                        _contextProvider.Value.ReleaseProjectContext(_currentAggregateProjectContext);
                    }
                });
            }
        }

        private void DisposeAndClearSubscriptions()
        {
            lock (_linksLock)
            {
                foreach (Lazy<ICrossTargetSubscriber> subscriber in Subscribers)
                {
                    subscriber.Value.ReleaseSubscriptions();
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

        private Task ExecuteWithinLockAsync(Action action)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, action);
        }

        private Task<T> ExecuteWithinLockAsync<T>(Func<T> func)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, func);
        }
    }
}
