// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

[Export(typeof(UnconfiguredSetupComponentDataSource))]
[method: ImportingConstructor]
internal sealed class UnconfiguredSetupComponentDataSource(UnconfiguredProject unconfiguredProject, IActiveConfigurationGroupService activeConfigurationGroupService)
    : ChainedProjectValueDataSourceBase<UnconfiguredSetupComponentSnapshot>(projectService: unconfiguredProject.ProjectService, synchronousDisposal: false, registerDataSource: false)
{
    protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<UnconfiguredSetupComponentSnapshot>> targetBlock)
    {
        // Join data across configurations into a single update.
        var joinBlock = new ConfiguredProjectDataSourceJoinBlock<ConfiguredSetupComponentSnapshot>(
            configuredProject => configuredProject.Services.ExportProvider.GetExportedValue<ConfiguredSetupComponentDataSource>(),
            JoinableFactory,
            unconfiguredProject);

        UnconfiguredSetupComponentSnapshot snapshot = UnconfiguredSetupComponentSnapshot.Empty;

        // Combine data across all configurations.
        var mergeBlock = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<IReadOnlyCollection<ConfiguredSetupComponentSnapshot>>, IProjectVersionedValue<UnconfiguredSetupComponentSnapshot>>(
            Transform,
            nameFormat: $"Merge {nameof(ConfiguredSetupComponentSnapshot)} {{1}}",
            skipIntermediateInputData: true,
            skipIntermediateOutputData: true); // skip data if we fall behind

        mergeBlock.LinkTo(
            targetBlock,
            DataflowOption.PropagateCompletion);

        joinBlock.LinkTo(mergeBlock, DataflowOption.PropagateCompletion);

        JoinUpstreamDataSources(activeConfigurationGroupService.ActiveConfiguredProjectGroupSource);

        // Link a data source of active ConfiguredProjects into the join block.
        return activeConfigurationGroupService.ActiveConfiguredProjectGroupSource.SourceBlock.LinkTo(
            joinBlock,
            DataflowOption.PropagateCompletion);

        ProjectVersionedValue<UnconfiguredSetupComponentSnapshot> Transform(IProjectVersionedValue<IReadOnlyCollection<ConfiguredSetupComponentSnapshot>> update)
        {
            snapshot = UnconfiguredSetupComponentSnapshot.Update(snapshot, update.Value);
            return new(snapshot, update.DataSourceVersions);
        }
    }

    [Export(typeof(ConfiguredSetupComponentDataSource))]
    [method: ImportingConstructor]
    private sealed class ConfiguredSetupComponentDataSource(ConfiguredProject configuredProject, IProjectSubscriptionService projectSubscriptionService)
        : ChainedProjectValueDataSourceBase<ConfiguredSetupComponentSnapshot>(projectService: configuredProject.UnconfiguredProject.ProjectService, synchronousDisposal: false, registerDataSource: false)
    {
        private static readonly IImmutableSet<string> s_buildRuleNames = ImmutableStringHashSet.EmptyRuleNames.Add(SuggestedVisualStudioComponentId.SchemaName);

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ConfiguredSetupComponentSnapshot>> targetBlock)
        {
            ConfiguredSetupComponentSnapshot snapshot = ConfiguredSetupComponentSnapshot.Empty;

            var transform = DataflowBlockSlim.CreateTransformBlock<
                IProjectVersionedValue<(IProjectSubscriptionUpdate Update, IProjectCapabilitiesSnapshot Capabilities)>,
                IProjectVersionedValue<ConfiguredSetupComponentSnapshot>>(
                    Transform,
                    nameFormat: $"{nameof(ConfiguredSetupComponentDataSource)} transform {{1}}",
                    skipIntermediateInputData: false,
                    skipIntermediateOutputData: true); // skip data if we fall behind

            transform.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            JoinUpstreamDataSources(projectSubscriptionService.ProjectBuildRuleSource, configuredProject.Capabilities);

            return ProjectDataSources.SyncLinkTo(
                projectSubscriptionService.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(s_buildRuleNames)),
                configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                target: transform,
                linkOptions: DataflowOption.PropagateCompletion);

            IProjectVersionedValue<ConfiguredSetupComponentSnapshot> Transform(IProjectVersionedValue<(IProjectSubscriptionUpdate Update, IProjectCapabilitiesSnapshot Capabilities)> update)
            {
                // Apply the update. Note that this may return the same instance as before, however because
                // we join the output of this block with that of other blocks, we must always return a value
                // with the latest versions.
                snapshot = snapshot.Update(update.Value.Update, update.Value.Capabilities);

                return new ProjectVersionedValue<ConfiguredSetupComponentSnapshot>(snapshot, update.DataSourceVersions);
            }
        }
    }
}
