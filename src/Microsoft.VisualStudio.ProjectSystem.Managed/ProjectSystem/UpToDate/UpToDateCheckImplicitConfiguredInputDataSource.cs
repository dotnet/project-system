// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
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
            IProjectItemSchemaService projectItemSchemaService)
            : base(containingProject, synchronousDisposal: true, registerDataSource: false)
        {
            _configuredProject = containingProject;
            _projectItemSchemaService = projectItemSchemaService;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> targetBlock)
        {
            Assumes.Present(_configuredProject.Services.ProjectSubscription);

            // Initial state is empty. We will evolve this reference over time, updating it iteratively
            // on each new data update.
            UpToDateCheckImplicitConfiguredInput state = UpToDateCheckImplicitConfiguredInput.Empty;

            IPropagatorBlock<IProjectVersionedValue<UpdateValues>, IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>> transformBlock
                = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<UpdateValues>, IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>>(Transform);

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

            IProjectVersionedValue<UpToDateCheckImplicitConfiguredInput> Transform(IProjectVersionedValue<UpdateValues> e)
            {
                var snapshot = e.Value.Item3 as IProjectSnapshot2;
                Assumes.NotNull(snapshot);

                state = state.Update(
                    jointRuleUpdate: e.Value.Item1,
                    sourceItemsUpdate: e.Value.Item2,
                    projectSnapshot: snapshot,
                    projectItemSchema: e.Value.Item4,
                    projectCatalogSnapshot: e.Value.Item5,
                    configuredProjectVersion: e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion]);

                return new ProjectVersionedValue<UpToDateCheckImplicitConfiguredInput>(state, e.DataSourceVersions);
            }
        }
    }
}
