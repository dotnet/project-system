// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
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

        private readonly Lazy<ImmutableHashSet<string>> _evaluationRuleNames;
        private readonly Lazy<ImmutableHashSet<string>> _buildRuleNames;
        private readonly Lazy<string[]> _jointRuleNames;
        private readonly Lazy<JointRuleDataflowLinkOptions> _jointRuleDataflowLinkOptions;

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

            // Call these lazily. We need to wait for MEF to initialize the handlers collection before we use it.
            _evaluationRuleNames = new Lazy<ImmutableHashSet<string>>(() => GetRuleNames(isEvaluation: true));
            _buildRuleNames = new Lazy<ImmutableHashSet<string>>(() => GetRuleNames(isEvaluation: false));
            _jointRuleNames = new Lazy<string[]>(() => _evaluationRuleNames.Value.Concat(_buildRuleNames.Value).ToArray());
            _jointRuleDataflowLinkOptions = new Lazy<JointRuleDataflowLinkOptions>(() => new() { EvaluationRuleNames = _evaluationRuleNames.Value, BuildRuleNames = _buildRuleNames.Value });

            ImmutableHashSet<string> GetRuleNames(bool isEvaluation)
            {
                ImmutableHashSet<string>.Builder rules = ImmutableHashSet.CreateBuilder<string>(StringComparers.RuleNames);

                foreach (Lazy<IDependenciesRuleHandler, IOrderPrecedenceMetadataView> item in _handlers)
                {
                    rules.Add(isEvaluation
                        ? item.Value.EvaluatedRuleName
                        : item.Value.ResolvedRuleName);
                }

                return rules.ToImmutable();
            }
        }

        public override void AddSubscriptions(AggregateCrossTargetProjectContext projectContext)
        {
            _treeTelemetryService.InitializeTargetFrameworkRules(projectContext.TargetFrameworks, _jointRuleNames.Value);

            base.AddSubscriptions(projectContext);
        }

        protected override void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService)
        {
            Subscribe(
                configuredProject,
                subscriptionService.ProjectRuleSource,
                "CrossTarget Evaluation Input: {1}",
                SyncLink,
                ruleNames: _evaluationRuleNames.Value.ToArray());

            Subscribe(
                configuredProject,
                subscriptionService.JointRuleSource,
                "CrossTarget Joint Input: {1}",
                SyncLink,
                options: _jointRuleDataflowLinkOptions.Value);

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
