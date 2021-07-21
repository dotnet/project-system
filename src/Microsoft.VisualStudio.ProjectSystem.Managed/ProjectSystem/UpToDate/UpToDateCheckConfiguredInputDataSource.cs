// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
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
            // Provides the set of implicitly active configured projects
            IProjectValueDataSource<IConfigurationGroup<ConfiguredProject>> activeConfiguredProjectsSource = _activeConfigurationGroupService.ActiveConfiguredProjectGroupSource;

            // Aggregates implicitly active UpToDateCheckImplicitConfiguredInput inputs from their sources
            var restoreConfiguredInputSource = new UnwrapCollectionChainedProjectValueDataSource<IReadOnlyCollection<ConfiguredProject>, UpToDateCheckImplicitConfiguredInput>(
                _configuredProject,
                projects => projects.Select(project => project.Services.ExportProvider.GetExportedValueOrDefault<IUpToDateCheckImplicitConfiguredInputDataSource>())
                    .WhereNotNull() // Filter out any configurations which don't have this export
                    .Select(DropConfiguredProjectVersions),
                includeSourceVersions: true);

            // Dataflow from two configurations can depend on a same unconfigured level data source, and processes it at a different speed.
            // Introduce a forward-only block to prevent regressions in versions.
            var forwardOnlyBlock = ProjectDataSources.CreateDataSourceVersionForwardOnlyFilteringBlock<IReadOnlyCollection<UpToDateCheckImplicitConfiguredInput>>();

            DisposableValue<ISourceBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>>>
                mergeBlock = forwardOnlyBlock.TransformWithNoDelta(update => update.Derive(MergeInputs));

            JoinUpstreamDataSources(restoreConfiguredInputSource, activeConfiguredProjectsSource);

            return new DisposableBag
            {
                restoreConfiguredInputSource,
                activeConfiguredProjectsSource.SourceBlock.LinkTo(restoreConfiguredInputSource, DataflowOption.PropagateCompletion),
                restoreConfiguredInputSource.SourceBlock.LinkTo(forwardOnlyBlock, DataflowOption.PropagateCompletion),
                mergeBlock,
                mergeBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion)
            };

            IProjectValueDataSource<UpToDateCheckImplicitConfiguredInput> DropConfiguredProjectVersions(IUpToDateCheckImplicitConfiguredInputDataSource dataSource)
            {
                // Wrap it in a data source that will drop project version and identity versions so as they will never agree
                // on these versions as they are unique to each configuration. They'll be consistent by all other versions.
                return new DropConfiguredProjectVersionDataSource<UpToDateCheckImplicitConfiguredInput>(_configuredProject.UnconfiguredProject, dataSource);
            }

            static UpToDateCheckConfiguredInput MergeInputs(IReadOnlyCollection<UpToDateCheckImplicitConfiguredInput> inputs)
            {
                return new(inputs.ToImmutableArray());
            }
        }
    }
}
