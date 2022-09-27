// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    /// <summary>
    /// Base class for <see cref="IDependencyCrossTargetSubscriber"/> implementations.
    /// </summary>
    internal abstract class DependencyRulesSubscriberBase<T> : OnceInitializedOnceDisposedUnderLockAsync, IDependencyCrossTargetSubscriber
    {
        private readonly IUnconfiguredProjectTasksService _tasksService;

        private DependenciesSnapshotProvider? _provider;
        private IDisposable? _subscriptions;

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
                Assumes.Present(configuredProject.Services.ProjectSubscription);

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
            Func<(ISourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> Intermediate, ITargetBlock<IProjectVersionedValue<T>> Action, string[] RuleNames), IDisposable> syncLink)
        {
            ITargetBlock<IProjectVersionedValue<T>> actionBlock =
                DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<T>>(
                    e => OnProjectChangedAsync(configuredProject, e.Value),
                    configuredProject.UnconfiguredProject,
                    nameFormat: nameFormat);

            _subscriptions = syncLink((dataSource.SourceBlock, actionBlock, ruleNames));
        }

        private Task OnProjectChangedAsync(ConfiguredProject configuredProject, T e)
        {
            // Ensure updates don't overlap and that we aren't disposed during the update without cleaning up properly
            return ExecuteUnderLockAsync(
                async cancellationToken =>
                {
                    if (IsDisposing || IsDisposed)
                    {
                        return;
                    }

                    Assumes.True(IsInitialized);

                    // Ensure the project's capabilities don't change during the update
                    using (ProjectCapabilitiesContext.CreateIsolatedContext(configuredProject, capabilities: GetCapabilitiesSnapshot(e)))
                    {
                        AggregateCrossTargetProjectContext? currentAggregateContext = await _provider!.GetCurrentAggregateProjectContextAsync(cancellationToken);

                        if (currentAggregateContext is null)
                        {
                            return;
                        }

                        // Get the target framework to update for this change.
                        TargetFramework? targetFrameworkToUpdate = currentAggregateContext.GetProjectFramework(GetProjectConfiguration(e));

                        if (targetFrameworkToUpdate is null)
                        {
                            return;
                        }

                        Handle(configuredProject.UnconfiguredProject.FullPath, currentAggregateContext, targetFrameworkToUpdate, e, cancellationToken);
                    }
                },
                _tasksService.UnloadCancellationToken);
        }

        protected abstract void Handle(string projectFullPath, AggregateCrossTargetProjectContext currentAggregateContext, TargetFramework targetFrameworkToUpdate, T e, CancellationToken cancellationToken);

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

        protected void RaiseDependenciesChanged(TargetFramework targetFramework, IDependenciesChanges? changes, AggregateCrossTargetProjectContext currentAggregateContext, IProjectCatalogSnapshot catalogSnapshot)
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
