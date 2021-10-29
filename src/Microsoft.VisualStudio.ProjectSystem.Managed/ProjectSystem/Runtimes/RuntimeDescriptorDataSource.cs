// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Runtimes
{
    [Export(typeof(IRuntimeDescriptorDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class RuntimeDescriptorDataSource : ChainedProjectValueDataSourceBase<ISet<RuntimeDescriptor>>, IRuntimeDescriptorDataSource
    {
        private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
                                                                        .Add(MissingSdkRuntime.SchemaName);

        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public RuntimeDescriptorDataSource(
            ConfiguredProject project,
            IProjectSubscriptionService projectSubscriptionService)
            : base(project, synchronousDisposal: true, registerDataSource: false)
        {
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ISet<RuntimeDescriptor>>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.ProjectBuildRuleSource;

            // Transform the changes from design-time build -> sdk runtime component data
            DisposableValue<ISourceBlock<IProjectVersionedValue<ISet<RuntimeDescriptor>>>> transformBlock =
                source.SourceBlock.TransformWithNoDelta(update => update.Derive(u => CreateRuntimeDescriptor(u.CurrentState)),
                                                        suppressVersionOnlyUpdates: false,
                                                        ruleNames: s_rules);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private ISet<RuntimeDescriptor> CreateRuntimeDescriptor(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
        {
            IProjectRuleSnapshot missingSdkRuntimes = currentState.GetSnapshotOrEmpty(MissingSdkRuntime.SchemaName);

            if (missingSdkRuntimes.Items.Count == 0)
            {
                return ImmutableHashSet<RuntimeDescriptor>.Empty;
            }

            var runtimeDescriptors = missingSdkRuntimes.Items.Select(item =>
            {
                return new RuntimeDescriptor(item.Key);
            });

            return new HashSet<RuntimeDescriptor>(runtimeDescriptors);
        }
    }
}
