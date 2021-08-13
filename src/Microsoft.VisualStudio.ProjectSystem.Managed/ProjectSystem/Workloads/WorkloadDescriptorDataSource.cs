// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    [Export(typeof(IWorkloadDescriptorDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class WorkloadDescriptorDataSource : ChainedProjectValueDataSourceBase<WorkloadDescriptor>, IWorkloadDescriptorDataSource
    {
        private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
                                                                        .Add(SuggestedWorkload.SchemaName); // From Evaluation

        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public WorkloadDescriptorDataSource(
            ConfiguredProject project,
            IProjectSubscriptionService projectSubscriptionService)
            : base(project, synchronousDisposal: true, registerDataSource: false)
        {
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<WorkloadDescriptor>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.JointRuleSource;

            // Transform the changes from evaluation/design-time build -> workload data
            DisposableValue<ISourceBlock<IProjectVersionedValue<WorkloadDescriptor>>> transformBlock =
                source.SourceBlock.TransformWithNoDelta(update => update.Derive(u => CreateWorkloadDescriptor(u.CurrentState)),
                                                        suppressVersionOnlyUpdates: false,    // We need to coordinate these at the unconfigured-level
                                                        ruleNames: s_rules);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private WorkloadDescriptor CreateWorkloadDescriptor(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
        {
            IProjectRuleSnapshot suggestedWorkloads = currentState.GetSnapshotOrEmpty(SuggestedWorkload.SchemaName);

            if (suggestedWorkloads.Items.Any())
            {
                System.Diagnostics.Debug.Assert(suggestedWorkloads.Items.Count == 1, $"Expected only 1 SuggestedWorkload item but found {suggestedWorkloads.Items.Count}");
                var workload = suggestedWorkloads.Items.First();
                string workloadName = workload.Key;

                if (workload.Value.TryGetValue(SuggestedWorkload.VisualStudioComponentIdProperty, out string vsComponentId))
                {
                    if (!string.IsNullOrWhiteSpace(workloadName) && !string.IsNullOrWhiteSpace(vsComponentId))
                    {
                        return new WorkloadDescriptor(workloadName, vsComponentId);
                    }
                }
            }

            return WorkloadDescriptor.Empty;
        }
    }
}
