// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IConfiguredProjectRetargetingDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ConfiguredProjectRetargetingDataSource : ChainedProjectValueDataSourceBase<IImmutableList<TargetDescriptionBase>>, IConfiguredProjectRetargetingDataSource
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        internal ConfiguredProjectRetargetingDataSource(ConfiguredProject project,
                                                        IProjectSubscriptionService projectSubscriptionService)
            : base(project.Services)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;

            ProjectRetargetCheckProviders = new OrderPrecedenceImportCollection<IProjectRetargetCheckProvider>(projectCapabilityCheckProvider: project);
        }

        protected override UnconfiguredProject? ContainingProject => _project.UnconfiguredProject;

        /// <summary>
        /// Import the LaunchTargetProviders which know how to run profiles
        /// </summary>
        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectRetargetCheckProvider> ProjectRetargetCheckProviders { get; }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<IImmutableList<TargetDescriptionBase>>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.ProjectRuleSource;

            // Transform the changes from evaluation/design-time build -> restore data
            DisposableValue<ISourceBlock<IProjectVersionedValue<IImmutableList<TargetDescriptionBase>>>> transformBlock = source.SourceBlock.TransformWithNoDelta(CheckForProjectRetarget,
                                                                                                                                                                  suppressVersionOnlyUpdates: false);    // We need to coordinate these at the unconfigured-level

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private IProjectVersionedValue<IImmutableList<TargetDescriptionBase>> CheckForProjectRetarget(IProjectVersionedValue<IProjectSubscriptionUpdate> arg)
        {
            ImmutableList<TargetDescriptionBase>.Builder changes = ImmutableList.CreateBuilder<TargetDescriptionBase>();

            foreach (IProjectRetargetCheckProvider provider in ProjectRetargetCheckProviders.ExtensionValues())
            {
                TargetDescriptionBase? change = provider.Check(arg.Value.CurrentState);
                if (change != null)
                {
                    changes.Add(change);
                }
            }

            return new ProjectVersionedValue<IImmutableList<TargetDescriptionBase>>(changes.ToImmutable(), arg.DataSourceVersions);
        }
    }
}
