// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

using UpdateValues = System.ValueTuple<
    Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate,
    Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate,
    Microsoft.VisualStudio.ProjectSystem.IProjectSnapshot,
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

        private static ImmutableHashSet<string> ProjectPropertiesSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(ConfigurationGeneral.SchemaName)
            .Add(ResolvedAnalyzerReference.SchemaName)
            .Add(ResolvedCompilationReference.SchemaName)
            .Add(CopyUpToDateMarker.SchemaName)
            .Add(UpToDateCheckInput.SchemaName)
            .Add(UpToDateCheckOutput.SchemaName)
            .Add(UpToDateCheckBuilt.SchemaName);

        [ImportingConstructor]
        public UpToDateCheckImplicitConfiguredInputDataSource(
            ConfiguredProject containingProject,
            IProjectItemSchemaService projectItemSchemaService,
            [Import(AllowDefault = true)] IUpToDateCheckStatePersistence? persistentState)
            : base(containingProject, synchronousDisposal: false, registerDataSource: false)
        {
            _configuredProject = containingProject;
            _projectItemSchemaService = projectItemSchemaService;
            _persistentState = persistentState;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> targetBlock)
        {
            Assumes.Present(_configuredProject.Services.ProjectSubscription);

            bool attemptedStateRestore = false;

            // Initial state is empty. We will evolve this reference over time, updating it iteratively
            // on each new data update.
            UpToDateCheckImplicitConfiguredInput state = UpToDateCheckImplicitConfiguredInput.Empty;

            IPropagatorBlock<IProjectVersionedValue<UpdateValues>, IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> transformBlock
                = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<UpdateValues>, IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>>(TransformAsync);

            IProjectValueDataSource<IProjectSubscriptionUpdate> source1 = _configuredProject.Services.ProjectSubscription.JointRuleSource;
            IProjectValueDataSource<IProjectSubscriptionUpdate> source2 = _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource;
            IProjectValueDataSource<IProjectSnapshot> source3 = _configuredProject.Services.ProjectSubscription.ProjectSource;
            IProjectItemSchemaService source4 = _projectItemSchemaService;
            IProjectValueDataSource<IProjectCatalogSnapshot> source5 = _configuredProject.Services.ProjectSubscription.ProjectCatalogSource;

            return new DisposableBag
            {
                // Sync-link various sources to our transform block
                ProjectDataSources.SyncLinkTo(
                    source1.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectPropertiesSchemas)),
                    source2.SourceBlock.SyncLinkOptions(),
                    source3.SourceBlock.SyncLinkOptions(),
                    source4.SourceBlock.SyncLinkOptions(),
                    source5.SourceBlock.SyncLinkOptions(),
                    target: transformBlock,
                    linkOptions: DataflowOption.PropagateCompletion,
                    CancellationToken.None),

                // Link the transform block to our target block
                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion),

                JoinUpstreamDataSources(source1, source2, source3, source4, source5)
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
                        (int ItemHash, DateTime InputsChangedAtUtc)? restoredState =
                            await JoinableFactory.RunAsync(() => _persistentState.RestoreStateAsync(_configuredProject.UnconfiguredProject.FullPath, _configuredProject.ProjectConfiguration.Dimensions));

                        if (restoredState is not null)
                        {
                            state = state.WithRestoredState(restoredState.Value.ItemHash, restoredState.Value.InputsChangedAtUtc);
                        }
                    }
                }

                int? priorItemHash = state.ItemHash;
                DateTime priorLastItemsChangedAtUtc = state.LastItemsChangedAtUtc;

                var snapshot = e.Value.Item3 as IProjectSnapshot2;
                Assumes.NotNull(snapshot);

                state = state.Update(
                    jointRuleUpdate: e.Value.Item1,
                    sourceItemsUpdate: e.Value.Item2,
                    projectSnapshot: snapshot,
                    projectItemSchema: e.Value.Item4,
                    projectCatalogSnapshot: e.Value.Item5);

                if (_persistentState != null && (priorItemHash != state.ItemHash || priorLastItemsChangedAtUtc != state.LastItemsChangedAtUtc))
                {
                    // The input hash is always non-null after calling Update.
                    Assumes.NotNull(state.ItemHash);

                    _persistentState.StoreState(_configuredProject.UnconfiguredProject.FullPath, _configuredProject.ProjectConfiguration.Dimensions, state.ItemHash.Value, state.LastItemsChangedAtUtc);
                }

                return new ProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>(state, e.DataSourceVersions);
            }
        }
    }
}
