// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

using UpdateValues = System.ValueTuple<
    Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate,
    Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate,
    Microsoft.VisualStudio.ProjectSystem.IProjectItemSchema,
    Microsoft.VisualStudio.ProjectSystem.Properties.IProjectCatalogSnapshot>;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <inheritdoc cref="IUpToDateCheckImplicitConfiguredInputDataSource" />
    [Export(typeof(IUpToDateCheckImplicitConfiguredInputDataSource))]
    [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
    internal sealed class UpToDateCheckImplicitConfiguredInputDataSource : ChainedProjectValueDataSourceBase<UpToDateCheckImplicitConfiguredInput>, IUpToDateCheckImplicitConfiguredInputDataSource
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectItemSchemaService _projectItemSchemaService;
        private readonly IUpToDateCheckStatePersistence? _persistentState;
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly ICopyItemAggregator _copyItemAggregator;

        /// <summary>
        /// The rules of data we want from <see cref="IProjectSubscriptionService.JointRuleSource"/>.
        /// </summary>
        private static ImmutableHashSet<string> JointRuleSchemaNames => ImmutableStringHashSet.EmptyOrdinal
            .Add(ConfigurationGeneral.SchemaName)
            .Add(ResolvedAnalyzerReference.SchemaName)
            .Add(ResolvedCompilationReference.SchemaName)
            .Add(CopyUpToDateMarker.SchemaName)
            .Add(UpToDateCheckInput.SchemaName)
            .Add(UpToDateCheckOutput.SchemaName)
            .Add(UpToDateCheckBuilt.SchemaName)
            .Add(CopyToOutputDirectoryItem.SchemaName)
            .Add(ResolvedProjectReference.SchemaName);

        [ImportingConstructor]
        public UpToDateCheckImplicitConfiguredInputDataSource(
            ConfiguredProject containingProject,
            IProjectItemSchemaService projectItemSchemaService,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
            ICopyItemAggregator copyItemAggregator,
            [Import(AllowDefault = true)] IUpToDateCheckStatePersistence? persistentState)
            : base(containingProject, synchronousDisposal: false, registerDataSource: false)
        {
            _configuredProject = containingProject;
            _projectItemSchemaService = projectItemSchemaService;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _copyItemAggregator = copyItemAggregator;
            _persistentState = persistentState;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> targetBlock)
        {
            Assumes.Present(_configuredProject.Services.ProjectSubscription);

            bool attemptedStateRestore = false;

            // Initial state is empty. We will evolve this reference over time, updating it iteratively
            // on each new data update.
            UpToDateCheckImplicitConfiguredInput state = UpToDateCheckImplicitConfiguredInput.CreateEmpty(_configuredProject.ProjectConfiguration);

            IPropagatorBlock<IProjectVersionedValue<UpdateValues>, IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> transformBlock
                = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<UpdateValues>, IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>>(TransformAsync);

            IProjectValueDataSource<IProjectSubscriptionUpdate> source1 = _configuredProject.Services.ProjectSubscription.JointRuleSource;
            IProjectValueDataSource<IProjectSubscriptionUpdate> source2 = _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource;
            IProjectItemSchemaService source3 = _projectItemSchemaService;
            IProjectValueDataSource<IProjectCatalogSnapshot> source4 = _configuredProject.Services.ProjectSubscription.ProjectCatalogSource;

            return new DisposableBag
            {
                // Sync-link various sources to our transform block
                ProjectDataSources.SyncLinkTo(
                    source1.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(JointRuleSchemaNames)),
                    source2.SourceBlock.SyncLinkOptions(),
                    source3.SourceBlock.SyncLinkOptions(),
                    source4.SourceBlock.SyncLinkOptions(),
                    target: transformBlock,
                    linkOptions: DataflowOption.PropagateCompletion,
                    CancellationToken.None),

                // Link the transform block to our target block
                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion),

                JoinUpstreamDataSources(source1, source2, source3, source4)
            };

            async Task<IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> TransformAsync(IProjectVersionedValue<UpdateValues> e)
            {
                if (!attemptedStateRestore)
                {
                    attemptedStateRestore = true;

                    if (_persistentState is not null)
                    {
                        // Restoring state requires the UI thread. We must use JTF.RunAsync here to ensure the UI
                        // thread is shared between related work and prevent deadlocks.
                        (int ItemHash, DateTime? InputsChangedAtUtc)? restoredState =
                            await JoinableFactory.RunAsync(() => _persistentState.RestoreItemStateAsync(_configuredProject.UnconfiguredProject.FullPath, _configuredProject.ProjectConfiguration.Dimensions, _projectAsynchronousTasksService.UnloadCancellationToken));

                        if (restoredState is not null)
                        {
                            state = state.WithRestoredState(restoredState.Value.ItemHash, restoredState.Value.InputsChangedAtUtc);
                        }
                    }
                }

                int? priorItemHash = state.ItemHash;
                DateTime? priorLastItemsChangedAtUtc = state.LastItemsChangedAtUtc;
                ProjectCopyData priorCopyData = state.ProjectCopyData;

                state = state.Update(
                    jointRuleUpdate: e.Value.Item1,
                    sourceItemsUpdate: e.Value.Item2,
                    projectItemSchema: e.Value.Item3,
                    projectCatalogSnapshot: e.Value.Item4);

                if (priorCopyData != state.ProjectCopyData)
                {
                    // If the FUTDC is disabled, we won't have valid copy items in the snapshot.
                    if (!state.IsDisabled)
                    {
                        _copyItemAggregator.SetProjectData(state.ProjectCopyData);
                    }
                }

                if (state.ItemHash is not null && _persistentState is not null && (priorItemHash != state.ItemHash || priorLastItemsChangedAtUtc != state.LastItemsChangedAtUtc))
                {
                    await _persistentState.StoreItemStateAsync(_configuredProject.UnconfiguredProject.FullPath, _configuredProject.ProjectConfiguration.Dimensions, state.ItemHash.Value, state.LastItemsChangedAtUtc, _projectAsynchronousTasksService.UnloadCancellationToken);
                }

                return new ProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>(state, e.DataSourceVersions);
            }
        }
    }
}
