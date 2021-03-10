// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using EventData = System.Tuple<
    Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate,
    Microsoft.VisualStudio.ProjectSystem.Properties.IProjectCatalogSnapshot,
    Microsoft.VisualStudio.ProjectSystem.IProjectCapabilitiesSnapshot>;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependencyRulesSubscriber : DependencyRulesSubscriberBase<EventData>
    {
        public const string DependencyRulesSubscriberContract = "DependencyRulesSubscriberContract";

        private readonly IDependencyTreeTelemetryService _treeTelemetryService;

        [ImportMany(DependencyRulesSubscriberContract)]
        private readonly OrderPrecedenceImportCollection<IDependenciesRuleHandler> _handlers;

        private readonly Lazy<string[]> _watchedEvaluationRules;
        private readonly Lazy<string[]> _watchedJointRules;

        [ImportingConstructor]
        public DependencyRulesSubscriber(
            IUnconfiguredProjectCommonServices commonServices,
            IUnconfiguredProjectTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(commonServices.ThreadingService, tasksService)
        {
            _handlers = new OrderPrecedenceImportCollection<IDependenciesRuleHandler>(
                projectCapabilityCheckProvider: commonServices.Project);

            _treeTelemetryService = treeTelemetryService;

            _watchedJointRules = new Lazy<string[]>(() => GetRuleNames(RuleSource.Joint));
            _watchedEvaluationRules = new Lazy<string[]>(() => GetRuleNames(RuleSource.Evaluation));

            string[] GetRuleNames(RuleSource source)
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

                return rules.ToArray();
            }
        }

        public override void AddSubscriptions(AggregateCrossTargetProjectContext projectContext)
        {
            _treeTelemetryService.InitializeTargetFrameworkRules(projectContext.TargetFrameworks, _watchedJointRules.Value);

            base.AddSubscriptions(projectContext);
        }

        protected override void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService)
        {
            Subscribe(
                configuredProject,
                subscriptionService.ProjectRuleSource,
                _watchedEvaluationRules.Value,
                "CrossTarget Evaluation Input: {1}",
                SyncLink);

            Subscribe(
                configuredProject,
                subscriptionService.JointRuleSource,
                _watchedJointRules.Value,
                "CrossTarget Joint Input: {1}",
                SyncLink);

            IDisposable SyncLink((ISourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> Intermediate, ITargetBlock<IProjectVersionedValue<EventData>> Action) blocks)
            {
                return ProjectDataSources.SyncLinkTo(
                    blocks.Intermediate.SyncLinkOptions(),
                    subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                    configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                    blocks.Action,
                    linkOptions: DataflowOption.PropagateCompletion);
            }
        }

        protected override IProjectCapabilitiesSnapshot GetCapabilitiesSnapshot(EventData e) => e.Item3;
        protected override ProjectConfiguration GetProjectConfiguration(EventData e) => e.Item1.ProjectConfiguration;

        protected override void Handle(
            string projectFullPath,
            AggregateCrossTargetProjectContext currentAggregateContext,
            TargetFramework targetFrameworkToUpdate,
            EventData e)
        {
            IProjectSubscriptionUpdate projectUpdate = e.Item1;
            IProjectCatalogSnapshot catalogSnapshot = e.Item2;

            // Broken design-time builds can produce updates containing no rule data.
            // Later code assumes that the requested rules are available.
            // If we see no rule data, return now.
            if (projectUpdate.ProjectChanges.Count == 0)
            {
                return;
            }

            // Create an object to track dependency changes.
            var changesBuilder = new DependenciesChangesBuilder();

            // Give each handler a chance to register dependency changes.
            foreach (Lazy<IDependenciesRuleHandler, IOrderPrecedenceMetadataView> handler in _handlers)
            {
                IProjectChangeDescription evaluation = projectUpdate.ProjectChanges[handler.Value.EvaluatedRuleName];
                IProjectChangeDescription? build = projectUpdate.ProjectChanges.GetValueOrDefault(handler.Value.ResolvedRuleName);

                handler.Value.Handle(projectFullPath, evaluation, build, targetFrameworkToUpdate, changesBuilder);
            }

            IDependenciesChanges? changes = changesBuilder.TryBuildChanges();

            // Notify subscribers of a change in dependency data.
            // NOTE even if changes is null, it's possible the catalog has changed. If we don't take the newer
            // catalog we end up retaining a reference to an old catalog, which in turn retains an old project
            // instance which can be very large.
            RaiseDependenciesChanged(targetFrameworkToUpdate, changes, currentAggregateContext, catalogSnapshot);

            // Record all the rules that have occurred
            _treeTelemetryService.ObserveTargetFrameworkRules(targetFrameworkToUpdate, projectUpdate.ProjectChanges.Keys);
        }
    }
}
