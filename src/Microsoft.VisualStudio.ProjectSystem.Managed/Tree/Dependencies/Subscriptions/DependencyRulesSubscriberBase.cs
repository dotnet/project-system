// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    /// <summary>
    /// Base class for <see cref="IDependencyCrossTargetSubscriber"/> implementations.
    /// </summary>
    internal abstract class DependencyRulesSubscriberBase : OnceInitializedOnceDisposedUnderLockAsync, IDependencyCrossTargetSubscriber
    {
        protected IUnconfiguredProjectTasksService TasksService { get; }

        protected DisposableBag? Subscriptions { get; set; }

        protected ICrossTargetSubscriptionsHost? Host { get; private set; }

        protected AggregateCrossTargetProjectContext? CurrentProjectContext { get; private set; }

        public event EventHandler<DependencySubscriptionChangedEventArgs>? DependenciesChanged;

        protected DependencyRulesSubscriberBase(
            IUnconfiguredProjectTasksService tasksService,
            JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
            TasksService = tasksService;
        }

        protected abstract void InitializeSubscriber(IProjectSubscriptionService subscriptionService);

        protected abstract void AddSubscriptionsInternal(AggregateCrossTargetProjectContext projectContext);

        public async Task InitializeSubscriberAsync(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService)
        {
            Host = host;

            await InitializeAsync();

            InitializeSubscriber(subscriptionService);
        }

        public void AddSubscriptions(AggregateCrossTargetProjectContext projectContext)
        {
            Requires.NotNull(projectContext, nameof(projectContext));

            CurrentProjectContext = projectContext;

            AddSubscriptionsInternal(projectContext);
        }

        public void ReleaseSubscriptions()
        {
            CurrentProjectContext = null;

            // We can't re-use the DisposableBag after disposing it, so null it out
            // to ensure we create a new one the next time we go to add subscriptions.
            Subscriptions?.Dispose();
            Subscriptions = null;
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task DisposeCoreUnderLockAsync(bool initialized)
        {
            ReleaseSubscriptions();

            return Task.CompletedTask;
        }

        protected void RaiseDependenciesChanged(ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes, AggregateCrossTargetProjectContext currentAggregateContext, IProjectCatalogSnapshot catalogSnapshot)
        {
            DependenciesChanged?.Invoke(
                this,
                new DependencySubscriptionChangedEventArgs(
                    currentAggregateContext.TargetFrameworks,
                    currentAggregateContext.ActiveTargetFramework,
                    catalogSnapshot,
                    changes));
        }
    }
}
