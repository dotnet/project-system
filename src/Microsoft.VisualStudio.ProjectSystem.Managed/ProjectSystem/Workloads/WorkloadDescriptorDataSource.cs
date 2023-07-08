// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    [Export(typeof(IWorkloadDescriptorDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class WorkloadDescriptorDataSource : ChainedProjectValueDataSourceBase<ISet<WorkloadDescriptor>>, IWorkloadDescriptorDataSource
    {
        private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
                                                                        .Add(SuggestedWorkload.SchemaName);

        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public WorkloadDescriptorDataSource(
            ConfiguredProject project,
            IProjectSubscriptionService projectSubscriptionService)
            : base(project, synchronousDisposal: true, registerDataSource: false)
        {
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ISet<WorkloadDescriptor>>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.ProjectBuildRuleSource;

            // Transform the changes from design-time build -> workload data
            DisposableValue<ISourceBlock<IProjectVersionedValue<ISet<WorkloadDescriptor>>>> transformBlock =
                source.SourceBlock.TransformWithNoDelta(update => update.Derive(u => CreateWorkloadDescriptor(u.CurrentState)),
                                                        suppressVersionOnlyUpdates: false,
                                                        ruleNames: s_rules);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private static ISet<WorkloadDescriptor> CreateWorkloadDescriptor(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
        {
            IProjectRuleSnapshot suggestedWorkloads = currentState.GetSnapshotOrEmpty(SuggestedWorkload.SchemaName);

            if (suggestedWorkloads.Items.Count == 0)
            {
                return ImmutableHashSet<WorkloadDescriptor>.Empty;
            }

            var workloadDescriptors = suggestedWorkloads.Items.Select(item =>
            {
                string workloadName = item.Key;

                if (!string.IsNullOrWhiteSpace(workloadName)
                    && (item.Value.TryGetStringProperty(SuggestedWorkload.VisualStudioComponentIdsProperty, out string? vsComponentIds)
                     || item.Value.TryGetStringProperty(SuggestedWorkload.VisualStudioComponentIdProperty, out vsComponentIds)))
                {
                    return new WorkloadDescriptor(workloadName, vsComponentIds);
                }

                return WorkloadDescriptor.Empty;
            });

            return new HashSet<WorkloadDescriptor>(workloadDescriptors);
        }
    }
}
