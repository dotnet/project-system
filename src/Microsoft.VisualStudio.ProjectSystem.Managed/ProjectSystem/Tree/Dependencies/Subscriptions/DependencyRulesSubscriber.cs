// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
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

        private (ImmutableArray<IDependenciesRuleHandler> Handlers, string[] EvaluationRuleNames, string[] JointRuleNames)? _state;

        private readonly HashSet<(TargetFramework TargetFramework, string ProviderType, string DependencyId)> _resolvedItems = new();

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
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Capture the set of handlers at this point and use that collection consistently from here on.
            // The imported handlers may change over time in response to dynamic project capabilities.
            ImmutableArray<IDependenciesRuleHandler> handlers = _handlers.ExtensionValues().ToImmutableArray();

            // Also capture the set of rule names from these handlers, for later use.
            string[] evaluationRuleNames = GetRuleNames(includeResolved: false);
            string[] jointRuleNames = GetRuleNames(includeResolved: true);

            _state = (handlers, evaluationRuleNames, jointRuleNames);

            return Task.CompletedTask;

            string[] GetRuleNames(bool includeResolved)
            {
                var rules = new HashSet<string>(StringComparers.RuleNames);

                foreach (IDependenciesRuleHandler handler in handlers)
                {
                    rules.Add(handler.EvaluatedRuleName);

                    if (includeResolved)
                    {
                        rules.Add(handler.ResolvedRuleName);
                    }
                }

                return rules.ToArray();
            }
        }

        protected override Task DisposeCoreUnderLockAsync(bool initialized)
        {
            if (initialized)
            {
                _state = null;
            }

            return base.DisposeCoreUnderLockAsync(initialized);
        }

        public override void AddSubscriptions(AggregateCrossTargetProjectContext projectContext)
        {
            Assumes.NotNull(_state);

            _treeTelemetryService.InitializeTargetFrameworkRules(projectContext.TargetFrameworks, _state.Value.JointRuleNames);

            base.AddSubscriptions(projectContext);
        }

        protected override void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService)
        {
            Assumes.NotNull(_state);

            Subscribe(
                configuredProject,
                subscriptionService.ProjectRuleSource,
                _state.Value.EvaluationRuleNames,
                "CrossTarget Evaluation Input: {1}",
                SyncLink);

            Subscribe(
                configuredProject,
                subscriptionService.JointRuleSource,
                _state.Value.JointRuleNames,
                "CrossTarget Joint Input: {1}",
                SyncLink);

            IDisposable SyncLink((ISourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> Source, ITargetBlock<IProjectVersionedValue<EventData>> Action, string[] RuleNames) state)
            {
                return ProjectDataSources.SyncLinkTo(
                    state.Source.SyncLinkOptions(DataflowOption.WithRuleNames(state.RuleNames)),
                    subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                    configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                    state.Action,
                    linkOptions: DataflowOption.PropagateCompletion);
            }
        }

        protected override IProjectCapabilitiesSnapshot GetCapabilitiesSnapshot(EventData e) => e.Item3;
        protected override ProjectConfiguration GetProjectConfiguration(EventData e) => e.Item1.ProjectConfiguration;

        protected override void Handle(
            string projectFullPath,
            AggregateCrossTargetProjectContext currentAggregateContext,
            TargetFramework targetFrameworkToUpdate,
            EventData e,
            CancellationToken cancellationToken)
        {
            Assumes.NotNull(_state);

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
            var changesBuilder = new DependenciesChangesBuilder(_resolvedItems);

            // Give each handler a chance to register dependency changes.
            foreach (IDependenciesRuleHandler handler in _state.Value.Handlers)
            {
                IProjectChangeDescription evaluationProjectChange = projectUpdate.ProjectChanges[handler.EvaluatedRuleName];
                IProjectChangeDescription? buildProjectChange = projectUpdate.ProjectChanges.GetValueOrDefault(handler.ResolvedRuleName);

                handler.Handle(projectFullPath, evaluationProjectChange, buildProjectChange, targetFrameworkToUpdate, changesBuilder);
            }

            IDependenciesChanges? changes = changesBuilder.TryBuildChanges();

            cancellationToken.ThrowIfCancellationRequested();

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
