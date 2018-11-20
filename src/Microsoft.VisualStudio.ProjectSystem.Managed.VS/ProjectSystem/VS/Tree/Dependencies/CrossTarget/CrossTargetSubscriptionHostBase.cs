// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal abstract class CrossTargetSubscriptionHostBase : EnsureOnceInitializedOnceDisposedAsync, ICrossTargetSubscriptionsHost
    {
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked correctly by the IDisposeable analyzer
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
        private readonly SemaphoreSlim _linksLock = new SemaphoreSlim(initialCount: 1);
#pragma warning restore CA2213
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly Lazy<IAggregateCrossTargetProjectContextProvider> _contextProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly List<IDisposable> _evaluationSubscriptionLinks = new List<IDisposable>();

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

            await InitializeAsync();

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
                    e => OnProjectChangedAsync(e, RuleHandlerType.Evaluation));

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

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Update project context and subscriptions.
            return UpdateProjectContextAndSubscriptionsAsync();
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await InitializeAsync();

            await OnProjectChangedCoreAsync(e);
        }

        private async Task OnProjectChangedCoreAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            if (HasTargetFrameworksChanged(e))
            {
                await UpdateProjectContextAndSubscriptionsAsync();
            }
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
            AggregateCrossTargetProjectContext previousContextToDispose = null;
            return ExecuteWithinLockAsync(async () =>
            {
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
                        if (_currentAggregateProjectContext.ActiveProjectContext.TargetFramework.Equals(newTargetFramework))
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
                    DisposeAggregateProjectContext(previousContextToDispose);
                }

                // Create new project context.
                _currentAggregateProjectContext = await _contextProvider.Value.CreateProjectContextAsync();

                OnAggregateContextChanged(previousContextToDispose, _currentAggregateProjectContext);

                return _currentAggregateProjectContext;
            });
        }

        private void DisposeAggregateProjectContext(AggregateCrossTargetProjectContext projectContext)
        {
            _contextProvider.Value.ReleaseProjectContext(projectContext);
        }

        private async Task AddSubscriptionsAsync(AggregateCrossTargetProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            await _commonServices.ThreadingService.SwitchToUIThread();

            await _tasksService.LoadedProjectAsync(async () =>
            {
                await _linksLock.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, () =>
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
                });
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
            using (_linksLock.DisposableWait())
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
