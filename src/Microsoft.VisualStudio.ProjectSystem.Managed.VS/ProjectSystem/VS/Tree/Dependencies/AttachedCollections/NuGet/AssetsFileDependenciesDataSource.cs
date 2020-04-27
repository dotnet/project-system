// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.NuGet
{
    [Export(typeof(IAssetsFileDependenciesDataSource))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class AssetsFileDependenciesDataSource : ChainedProjectValueDataSourceBase<AssetsFileDependenciesSnapshot>, IAssetsFileDependenciesDataSource
    {
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveConfiguredProjectSnapshotService _activeConfiguredProjectSnapshotService;

        [ImportingConstructor]
        public AssetsFileDependenciesDataSource(
            UnconfiguredProject unconfiguredProject,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveConfiguredProjectSnapshotService activeConfiguredProjectSnapshotService)
            : base(unconfiguredProject.Services, synchronousDisposal: false, registerDataSource: false)
        {
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeConfiguredProjectSnapshotService = activeConfiguredProjectSnapshotService;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<AssetsFileDependenciesSnapshot>> targetBlock)
        {
            JoinUpstreamDataSources(_activeConfiguredProjectSubscriptionService.ProjectRuleSource);
            JoinUpstreamDataSources(_activeConfiguredProjectSnapshotService);

            string? lastAssetsFilePath = null;
            DateTime lastTimestampUtc = DateTime.MinValue;
            AssetsFileDependenciesSnapshot lastSnapshot = AssetsFileDependenciesSnapshot.Empty;

            var intermediateBlock =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions { NameFormat = nameof(AssetsFileDependenciesDataSource) + " Intermediate: {1}" });

            IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> projectRuleSource
                = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock;

            IPropagatorBlock<IProjectVersionedValue<ValueTuple<IProjectSnapshot, IProjectSubscriptionUpdate>>, IProjectVersionedValue<AssetsFileDependenciesSnapshot>> transformBlock
                = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<ValueTuple<IProjectSnapshot, IProjectSubscriptionUpdate>>, IProjectVersionedValue<AssetsFileDependenciesSnapshot>>(Transform, skipIntermediateInputData: true, skipIntermediateOutputData: true);

            return new DisposableBag
            {
                // Subscribe to "ConfigurationGeneral" rule data
                projectRuleSource.LinkTo(
                    intermediateBlock,
                    ruleNames: ConfigurationGeneral.SchemaName,
                    suppressVersionOnlyUpdates: false,
                    linkOptions: DataflowOption.PropagateCompletion),

                // Sync link inputs, joining on versions, and passing joined data to our transform block
                ProjectDataSources.SyncLinkTo(
                    _activeConfiguredProjectSnapshotService.SourceBlock.SyncLinkOptions(),
                    intermediateBlock.SyncLinkOptions(),
                    transformBlock,
                    linkOptions: DataflowOption.PropagateCompletion),

                // Flow transformed data to the output/target
                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion)
            };

            IProjectVersionedValue<AssetsFileDependenciesSnapshot> Transform(IProjectVersionedValue<ValueTuple<IProjectSnapshot, IProjectSubscriptionUpdate>> update)
            {
                var projectSnapshot = (IProjectSnapshot2)update.Value.Item1;
                IProjectSubscriptionUpdate subscriptionUpdate = update.Value.Item2;

                string? path = null;
                DateTime timestampUtc = DateTime.MinValue;

                AssetsFileDependenciesSnapshot snapshot = lastSnapshot;

                if (subscriptionUpdate.CurrentState.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectRuleSnapshot ruleSnapshot))
                {
                    if (ruleSnapshot.Properties.TryGetValue(ConfigurationGeneral.ProjectAssetsFileProperty, out path))
                    {
                        if (path.Length != 0)
                        {
                            if (projectSnapshot.AdditionalDependentFileTimes == null || !projectSnapshot.AdditionalDependentFileTimes.TryGetValue(path, out timestampUtc))
                            {
                                try
                                {
                                    // In the usual case we won't need to read this time stamp manually as the file will be present
                                    // in AdditionalDependentFileTimes. If however we do need to read it, we may end up reading a
                                    // timestamp that's newer than the one evaluation would have reported.
                                    // Note however that we can only read the current content of the file, so even if we have a
                                    // timestamp provided, we may be reading a newer version of the file, and we may skip versions.
                                    // Consumers of this data source need to work within those limitations.
                                    // For the dependencies tree there is no problem.
                                    timestampUtc = File.GetLastWriteTimeUtc(path);
                                }
                                catch
                                {
                                    // ignore
                                }
                            }

                            if (path != lastAssetsFilePath || timestampUtc != lastTimestampUtc)
                            {
                                lastAssetsFilePath = path;
                                lastTimestampUtc = timestampUtc;

                                lastSnapshot = snapshot = lastSnapshot.UpdateFromAssetsFile(path);
                            }
                        }
                    }
                }

                return new ProjectVersionedValue<AssetsFileDependenciesSnapshot>(snapshot, update.DataSourceVersions);
            }
        }
    }
}
