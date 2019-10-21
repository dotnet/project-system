// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    /// <summary>
    /// Base class for <see cref="IDependencyCrossTargetSubscriber"/> implementations.
    /// </summary>
    internal abstract class DependencyRulesSubscriberBase<T> : OnceInitializedOnceDisposedUnderLockAsync, IDependencyCrossTargetSubscriber
    {
        private readonly IUnconfiguredProjectTasksService _tasksService;

        private DependenciesSnapshotProvider? _provider;
        private DisposableBag? _subscriptions;

        public event EventHandler<DependencySubscriptionChangedEventArgs>? DependenciesChanged;

        protected DependencyRulesSubscriberBase(
            IProjectThreadingService threadingService,
            IUnconfiguredProjectTasksService tasksService)
            : base(threadingService.JoinableTaskContext)
        {
            _tasksService = tasksService;
        }

        public Task InitializeSubscriberAsync(DependenciesSnapshotProvider provider)
        {
            _provider = provider;

            return InitializeAsync();
        }

        public virtual void AddSubscriptions(AggregateCrossTargetProjectContext projectContext)
        {
            Requires.NotNull(projectContext, nameof(projectContext));
            Assumes.True(IsInitialized);

            foreach (ConfiguredProject configuredProject in projectContext.InnerConfiguredProjects)
            {
                SubscribeToConfiguredProject(configuredProject, configuredProject.Services.ProjectSubscription);
            }
        }

        public void ReleaseSubscriptions()
        {
            // We can't re-use the DisposableBag after disposing it, so null it out
            // to ensure we create a new one the next time we go to add subscriptions.
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        protected abstract void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService);

        protected void Subscribe(
            ConfiguredProject configuredProject,
            IProjectValueDataSource<IProjectSubscriptionUpdate> dataSource,
            string[] ruleNames,
            string nameFormat,
            Func<(BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> Intermediate, ITargetBlock<IProjectVersionedValue<T>> Action), IDisposable> syncLink)
        {
            // Use an intermediate buffer block for project rule data to allow subsequent blocks
            // to only observe specific rule name(s).

            var intermediateBlock =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions
                    {
                        NameFormat = nameFormat
                    });

            ITargetBlock<IProjectVersionedValue<T>> actionBlock =
                DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<T>>(
                    e => OnProjectChangedAsync(configuredProject, e.Value),
                    new ExecutionDataflowBlockOptions
                    {
                        NameFormat = nameFormat
                    });

            _subscriptions ??= new DisposableBag();

            _subscriptions.Add(
                dataSource.SourceBlock.LinkTo(
                    intermediateBlock,
                    ruleNames: ruleNames,
                    suppressVersionOnlyUpdates: false,
                    linkOptions: DataflowOption.PropagateCompletion));

            _subscriptions.Add(syncLink((intermediateBlock, actionBlock)));
        }

        private Task OnProjectChangedAsync(ConfiguredProject configuredProject, T e)
        {
            if (IsDisposing || IsDisposed)
            {
                return Task.CompletedTask;
            }

            Assumes.True(IsInitialized);

            // Ensure updates don't overlap and that we aren't disposed during the update without cleaning up properly
            return ExecuteUnderLockAsync(token =>
            {
                // Ensure the project doesn't unload during the update
                return _tasksService.LoadedProjectAsync(async () =>
                {
                    // TODO pass TasksService.UnloadCancellationToken into Handle to reduce redundant work on unload

                    // Ensure the project's capabilities don't change during the update
                    using (ProjectCapabilitiesContext.CreateIsolatedContext(configuredProject, capabilities: GetCapabilitiesSnapshot(e)))
                    {
                        AggregateCrossTargetProjectContext? currentAggregateContext = await _provider!.GetCurrentAggregateProjectContextAsync();

                        if (currentAggregateContext == null)
                        {
                            return;
                        }

                        // Get the target framework to update for this change.
                        ITargetFramework? targetFrameworkToUpdate = currentAggregateContext.GetProjectFramework(GetProjectConfiguration(e));

                        if (targetFrameworkToUpdate == null)
                        {
                            return;
                        }

                        Handle(currentAggregateContext, targetFrameworkToUpdate, e);
                    }
                });
            });
        }

        protected abstract void Handle(AggregateCrossTargetProjectContext currentAggregateContext, ITargetFramework targetFrameworkToUpdate, T e);

        protected abstract IProjectCapabilitiesSnapshot GetCapabilitiesSnapshot(T e);
        protected abstract ProjectConfiguration GetProjectConfiguration(T e);

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task DisposeCoreUnderLockAsync(bool initialized)
        {
            ReleaseSubscriptions();

            return Task.CompletedTask;
        }

        protected void RaiseDependenciesChanged(ITargetFramework targetFramework, IDependenciesChanges changes, AggregateCrossTargetProjectContext currentAggregateContext, IProjectCatalogSnapshot catalogSnapshot)
        {
            DependenciesChanged?.Invoke(
                this,
                new DependencySubscriptionChangedEventArgs(
                    currentAggregateContext.TargetFrameworks,
                    currentAggregateContext.ActiveTargetFramework,
                    targetFramework,
                    changes,
                    catalogSnapshot));
        }
    }
}
