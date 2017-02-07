// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal abstract class CrossTargetRuleSubscriberBase<T> : OnceInitializedOnceDisposedAsync, ICrossTargetSubscriber where T : IRuleChangeContext
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
        private readonly List<IDisposable> _evaluationSubscriptionLinks;
        private readonly List<IDisposable> _designTimeBuildSubscriptionLinks;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private ICrossTargetSubscriptionsHost _host;

        public CrossTargetRuleSubscriberBase(
            IUnconfiguredProjectCommonServices commonServices,
            IProjectAsynchronousTasksService tasksService)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _tasksService = tasksService;
            _evaluationSubscriptionLinks = new List<IDisposable>();
            _designTimeBuildSubscriptionLinks = new List<IDisposable>();
        }

        protected abstract OrderPrecedenceImportCollection<ICrossTargetRuleHandler<T>> Handlers { get; }

        public void Initialize(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService)
        {
            _host = host;

            var watchedEvaluationRules = GetWatchedRules(RuleHandlerType.Evaluation);
            var watchedDesignTimeBuildRules = GetWatchedRules(RuleHandlerType.DesignTimeBuild);

            SubscribeToConfiguredProject(
                subscriptionService, watchedEvaluationRules, watchedDesignTimeBuildRules);
        }

        public void AddSubscriptions(AggregateCrossTargetProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            var watchedEvaluationRules = GetWatchedRules(RuleHandlerType.Evaluation);
            var watchedDesignTimeBuildRules = GetWatchedRules(RuleHandlerType.DesignTimeBuild);

            foreach (var configuredProject in newProjectContext.InnerConfiguredProjects)
            {
                SubscribeToConfiguredProject(
                    configuredProject.Services.ProjectSubscription, watchedEvaluationRules, watchedDesignTimeBuildRules);
            }
        }

        public Task ReleaseSubscriptionsAsync()
        {
            foreach (var link in _evaluationSubscriptionLinks.Concat(_designTimeBuildSubscriptionLinks))
            {
                link.Dispose();
            }

            _evaluationSubscriptionLinks.Clear();
            _designTimeBuildSubscriptionLinks.Clear();

            return Task.CompletedTask;
        }

        public async Task OnContextReleasedAsync(ITargetedProjectContext innerContext)
        {
            foreach (var handler in Handlers)
            {
                await handler.Value.OnContextReleasedAsync(innerContext).ConfigureAwait(false);
            }
        }

        private void SubscribeToConfiguredProject(
            IProjectSubscriptionService subscriptionService,
            IEnumerable<string> watchedEvaluationRules,
            IEnumerable<string> watchedDesignTimeBuildRules)
        {
            var intermediateBlockDesignTime =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "CrossTarget Intermediate DesignTime Input: {1}"
                    });

            var intermediateBlockEvaluation =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "CrossTarget Intermediate Evaluation Input: {1}"
                    });

            _designTimeBuildSubscriptionLinks.Add(
                subscriptionService.JointRuleSource.SourceBlock.LinkTo(
                    intermediateBlockDesignTime,
                  ruleNames: watchedDesignTimeBuildRules.Union(watchedEvaluationRules),
                  suppressVersionOnlyUpdates: true,
                  linkOptions: new DataflowLinkOptions { PropagateCompletion = true }));

            _evaluationSubscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    intermediateBlockEvaluation,
                    ruleNames: watchedEvaluationRules,
                    suppressVersionOnlyUpdates: true,
                    linkOptions: new DataflowLinkOptions { PropagateCompletion = true }));

            var actionBlockDesignTimeBuild =
                new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot>>>(
                    e => OnProjectChangedAsync(e, RuleHandlerType.DesignTimeBuild),
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "CrossTarget DesignTime Input: {1}"
                    });

            var actionBlockEvaluation =
                new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot>>>(
                     e => OnProjectChangedAsync(e, RuleHandlerType.Evaluation),
                     new ExecutionDataflowBlockOptions()
                     {
                         NameFormat = "CrossTarget Evaluation Input: {1}"
                     });

            _designTimeBuildSubscriptionLinks.Add(ProjectDataSources.SyncLinkTo(
                intermediateBlockDesignTime.SyncLinkOptions(),
                subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                actionBlockDesignTimeBuild,
                linkOptions: new DataflowLinkOptions { PropagateCompletion = true }));

            _evaluationSubscriptionLinks.Add(ProjectDataSources.SyncLinkTo(
                intermediateBlockEvaluation.SyncLinkOptions(),
                subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                actionBlockEvaluation,
                linkOptions: new DataflowLinkOptions { PropagateCompletion = true }));
        }

        private IEnumerable<string> GetWatchedRules(RuleHandlerType handlerType)
        {
            var supportedHandler = Handlers.Where(h => h.Value.SupportsHandlerType(handlerType));
            var uniqueRuleNames = new HashSet<string>(StringComparers.RuleNames);
            foreach (var handler in supportedHandler)
            {
                foreach (var ruleName in handler.Value.GetRuleNames(handlerType))
                {
                    uniqueRuleNames.Add(ruleName);
                }
            }
            return uniqueRuleNames;
        }

        private async Task OnProjectChangedAsync(
            IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot>> e,
            RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await InitializeAsync().ConfigureAwait(false);

            await _tasksService.LoadedProjectAsync(async () =>
            {
                if (_tasksService.UnloadCancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await HandleAsync(e, handlerType).ConfigureAwait(false);
            });
        }

        private async Task HandleAsync(
                    IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot>> e,
                    RuleHandlerType handlerType)
        {
            var currentAggregaceContext = await _host.GetCurrentAggregateProjectContext().ConfigureAwait(false);
            if (currentAggregaceContext == null)
            {
                return;
            }

            IProjectSubscriptionUpdate update = e.Value.Item1;
            IProjectCatalogSnapshot catalogs = e.Value.Item2;
            var handlers = Handlers.Select(h => h.Value)
                                   .Where(h => h.SupportsHandlerType(handlerType));

            // We need to process the update within a lock to ensure that we do not release this context during processing.
            // TODO: Enable concurrent execution of updates themeselves, i.e. two separate invocations of HandleAsync
            //       should be able to run concurrently. 
            using (await _gate.DisposableWaitAsync().ConfigureAwait(true))
            {
                // Get the inner workspace project context to update for this change.
                var projectContextToUpdate = currentAggregaceContext
                    .GetInnerProjectContext(update.ProjectConfiguration, out bool isActiveContext);
                if (projectContextToUpdate == null)
                {
                    return;
                }

                // Broken design time builds sometimes cause updates with no project changes and sometimes
                // cause updates with a project change that has no difference.
                // We handle the former case here, and the latter case is handled in the CommandLineItemHandler.
                if (update.ProjectChanges.Count == 0)
                {
                    if (handlerType == RuleHandlerType.DesignTimeBuild)
                    {
                        projectContextToUpdate.LastDesignTimeBuildSucceeded = false;
                    }

                    return;
                }

                var ruleChangeContext = CreateRuleChangeContext(
                    currentAggregaceContext.ActiveProjectContext.TargetFramework, catalogs);
                foreach (var handler in handlers)
                {
                    var builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
                    builder.AddRange(update.ProjectChanges.Where(
                        x => handler.GetRuleNames(handlerType).Contains(x.Key)));
                    var projectChanges = builder.ToImmutable();

                    if (handler.ReceiveUpdatesWithEmptyProjectChange
                        || projectChanges.Any(x => x.Value.Difference.AnyChanges))
                    {
                        await handler.HandleAsync(e,
                                                  projectChanges,
                                                  projectContextToUpdate,
                                                  isActiveContext,
                                                  ruleChangeContext)
                                     .ConfigureAwait(true);
                    }
                }

                await CompleteHandleAsync(ruleChangeContext).ConfigureAwait(false);
            }
        }

        protected abstract T CreateRuleChangeContext(ITargetFramework target, IProjectCatalogSnapshot catalogs);

        protected virtual Task CompleteHandleAsync(T ruleChangeContext)
        {
            return Task.CompletedTask;
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                await ReleaseSubscriptionsAsync().ConfigureAwait(false);
            }
        }
    }
}
