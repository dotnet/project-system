// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <inheritdoc cref="IUpToDateCheckConfiguredInputDataSource" />
    [Export(typeof(IUpToDateCheckConfiguredInputDataSource))]
    [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
    internal sealed class UpToDateCheckConfiguredInputDataSource : ChainedProjectValueDataSourceBase<UpToDateCheckConfiguredInput>, IUpToDateCheckConfiguredInputDataSource
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;

        [ImportingConstructor]
        public UpToDateCheckConfiguredInputDataSource(
            ConfiguredProject containingProject,
            IActiveConfigurationGroupService activeConfigurationGroupService)
            : base(containingProject, synchronousDisposal: false, registerDataSource: false)
        {
            _configuredProject = containingProject;
            _activeConfigurationGroupService = activeConfigurationGroupService;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>> targetBlock)
        {
            // Aggregates implicitly active UpToDateCheckImplicitConfiguredInput inputs from their sources
            var joinBlock = new ConfiguredProjectDataSourceJoinBlock<UpToDateCheckImplicitConfiguredInput>(
                project => project.Services.ExportProvider.GetExportedValueOrDefault<IUpToDateCheckImplicitConfiguredInputDataSource>(),
                JoinableFactory,
                _configuredProject.UnconfiguredProject);

            // Merges UpToDateCheckImplicitConfiguredInputs into a UpToDateCheckConfiguredInput
            DisposableValue<ISourceBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>>>
                mergeBlock = joinBlock.TransformWithNoDelta(update => update.Derive(MergeInputs));

            JoinUpstreamDataSources(_activeConfigurationGroupService.ActiveConfiguredProjectGroupSource);

            // Set the link up so that we publish changes to target block
            mergeBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return new DisposableBag
            {
                joinBlock,

                // Link the active configured projects to our join block
                _activeConfigurationGroupService.ActiveConfiguredProjectGroupSource.SourceBlock.LinkTo(joinBlock, DataflowOption.PropagateCompletion),
            };

            static UpToDateCheckConfiguredInput MergeInputs(IReadOnlyCollection<UpToDateCheckImplicitConfiguredInput> inputs)
            {
                return new(inputs.ToImmutableArray());
            }
        }
    }
}
