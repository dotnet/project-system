// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependencyRulesSubscriber : DependencyRulesSubscriberBase
    {
        public const string DependencyRulesSubscriberContract = "DependencyRulesSubscriberContract";

        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly IDependencyTreeTelemetryService _treeTelemetryService;

        [ImportMany(DependencyRulesSubscriberContract)]
        private readonly OrderPrecedenceImportCollection<IDependenciesRuleHandler> _handlers;

        [ImportingConstructor]
        public DependencyRulesSubscriber(
            IUnconfiguredProjectCommonServices commonServices,
            IUnconfiguredProjectTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(tasksService, commonServices.ThreadingService.JoinableTaskContext)
        {
            _handlers = new OrderPrecedenceImportCollection<IDependenciesRuleHandler>(
                projectCapabilityCheckProvider: commonServices.Project);

            _commonServices = commonServices;
            _treeTelemetryService = treeTelemetryService;
        }

        protected override void InitializeSubscriber(IProjectSubscriptionService subscriptionService)
        {
            IReadOnlyCollection<string> watchedEvaluationRules = GetRuleNames(RuleSource.Evaluation);
            IReadOnlyCollection<string> watchedJointRules = GetRuleNames(RuleSource.Joint);

            SubscribeToConfiguredProject(
                _commonServices.ActiveConfiguredProject, subscriptionService, watchedEvaluationRules, watchedJointRules);
        }

        protected override void AddSubscriptionsInternal(AggregateCrossTargetProjectContext projectContext)
        {
            IReadOnlyCollection<string> watchedEvaluationRules = GetRuleNames(RuleSource.Evaluation);
            IReadOnlyCollection<string> watchedJointRules = GetRuleNames(RuleSource.Joint);

            _treeTelemetryService.InitializeTargetFrameworkRules(projectContext.TargetFrameworks, watchedJointRules);

            foreach (ConfiguredProject configuredProject in projectContext.InnerConfiguredProjects)
            {
                SubscribeToConfiguredProject(
                    configuredProject, configuredProject.Services.ProjectSubscription, watchedEvaluationRules, watchedJointRules);
            }
        }

        private void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService,
            IReadOnlyCollection<string> watchedEvaluationRules,
            IReadOnlyCollection<string> watchedJointRules)
        {
            Subscribe(RuleSource.Evaluation, subscriptionService.ProjectRuleSource, watchedEvaluationRules);
            Subscribe(RuleSource.Joint, subscriptionService.JointRuleSource, watchedJointRules);

            void Subscribe(RuleSource source, IProjectValueDataSource<IProjectSubscriptionUpdate> dataSource, IReadOnlyCollection<string> ruleNames)
            {
                // Use intermediate buffer blocks for project rule data to allow subsequent blocks
                // to only observe specific rule name(s).

                var intermediateBlock =
                    new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                        new ExecutionDataflowBlockOptions
                        {
                            NameFormat = string.Intern($"CrossTarget Intermediate {source} Input: {{1}}")
                        });

                ITargetBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>>> actionBlock =
                    DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>>>(
                        e => OnProjectChangedAsync(e.DataSourceVersions, e.Value.Item1, e.Value.Item2, e.Value.Item3, configuredProject, source),
                        new ExecutionDataflowBlockOptions
                        {
                            NameFormat = string.Intern($"CrossTarget {source} Input: {{1}}")
                        });

                Subscriptions ??= new DisposableBag();

                Subscriptions.Add(
                    dataSource.SourceBlock.LinkTo(
                        intermediateBlock,
                        ruleNames: ruleNames,
                        suppressVersionOnlyUpdates: false,
                        linkOptions: DataflowOption.PropagateCompletion));

                Subscriptions.Add(ProjectDataSources.SyncLinkTo(
                    intermediateBlock.SyncLinkOptions(),
                    subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                    configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                    actionBlock,
                    linkOptions: DataflowOption.PropagateCompletion));
            }
        }

        private IReadOnlyCollection<string> GetRuleNames(RuleSource source)
        {
            var rules = new HashSet<string>(StringComparers.RuleNames);

            foreach (Lazy<IDependenciesRuleHandler, IOrderPrecedenceMetadataView> item in _handlers)
            {
                rules.Add(item.Value.EvaluatedRuleName);

                if (source == RuleSource.Joint)
                {
                    rules.Add(item.Value.ResolvedRuleName);
                }
            }

            return rules;
        }

        private async Task OnProjectChangedAsync(
            IImmutableDictionary<NamedIdentity, IComparable> versions,
            IProjectSubscriptionUpdate projectUpdate,
            IProjectCatalogSnapshot catalogSnapshot,
            IProjectCapabilitiesSnapshot capabilities,
            ConfiguredProject configuredProject,
            RuleSource source)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            // Ensure updates don't overlap and that we aren't disposed during the update without cleaning up properly
            await ExecuteUnderLockAsync(async token =>
            {
                // Ensure the project doesn't unload during the update
                await TasksService.LoadedProjectAsync(async () =>
                {
                    // TODO pass _tasksService.UnloadCancellationToken into HandleAsync to reduce redundant work on unload

                    // Ensure the project's capabilities don't change during the update
                    using (ProjectCapabilitiesContext.CreateIsolatedContext(configuredProject, capabilities))
                    {
                        await HandleAsync(versions, projectUpdate, catalogSnapshot, source);
                    }
                });
            });
        }

        private async Task HandleAsync(
            IImmutableDictionary<NamedIdentity, IComparable> versions,
            IProjectSubscriptionUpdate projectUpdate,
            IProjectCatalogSnapshot catalogSnapshot,
            RuleSource source)
        {
            AggregateCrossTargetProjectContext? currentAggregateContext = await Host!.GetCurrentAggregateProjectContextAsync();

            if (currentAggregateContext == null || CurrentProjectContext != currentAggregateContext)
            {
                return;
            }

            // Get the inner workspace project context to update for this change.
            ITargetFramework? targetFrameworkToUpdate = currentAggregateContext.GetProjectFramework(projectUpdate.ProjectConfiguration);

            if (targetFrameworkToUpdate == null)
            {
                return;
            }

            // Broken design time builds sometimes cause updates with no project changes and sometimes
            // cause updates with a project change that has no difference.
            // We handle the former case here, and the latter case is handled in the CommandLineItemHandler.
            if (projectUpdate.ProjectChanges.Count == 0)
            {
                return;
            }

            if (!projectUpdate.ProjectChanges.Any(x => x.Value.Difference.AnyChanges))
            {
                return;
            }

            // Create an object to track dependency changes.
            var changesBuilder = new CrossTargetDependenciesChangesBuilder();

            // Give each handler a chance to register dependency changes.
            foreach (Lazy<IDependenciesRuleHandler, IOrderPrecedenceMetadataView> handler in _handlers)
            {
                handler.Value.Handle(versions, projectUpdate.ProjectChanges, source, targetFrameworkToUpdate, changesBuilder);
            }

            ImmutableDictionary<ITargetFramework, IDependenciesChanges>? changes = changesBuilder.TryBuildChanges();

            if (changes != null)
            {
                // Notify subscribers of a change in dependency data
                RaiseDependenciesChanged(changes, currentAggregateContext, catalogSnapshot);
            }

            // Record all the rules that have occurred
            _treeTelemetryService.ObserveTargetFrameworkRules(targetFrameworkToUpdate, projectUpdate.ProjectChanges.Keys);
        }
    }
}
