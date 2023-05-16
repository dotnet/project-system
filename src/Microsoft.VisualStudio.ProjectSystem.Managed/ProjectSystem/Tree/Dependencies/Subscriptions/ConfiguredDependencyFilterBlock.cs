// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

internal static class ConfiguredDependencyFilterBlock
{
    /// <summary>
    /// Wraps a target block such that it may be safely combined with blocks from other slices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Messages passing through this adapter have <see cref="ProjectDataSources.ConfiguredProjectIdentity"/>
    /// and <see cref="ProjectDataSources.ConfiguredProjectVersion"/> versions removed, which unblocks SyncLink.
    /// Without removing these, data from different configured projects would not have versions in sync and no
    /// data would be produced.
    /// </para>
    /// <para>
    /// This block also prohibits completion from propagating. This allows a given slice to be removed and
    /// cleaned up without propagating its completion through to downstream blocks. Note that faults are
    /// propagated, as we always want to ensure dataflow blocks fault when errors occur so that we don't leave
    /// components waiting for updates that will never arrive (hang).
    /// </para>
    /// <para>
    /// This might go away in future if we introduce an "unwrap" block that's suitable for use with
    /// slices. It would build this logic in directly. That would be desirable over this, as currently this
    /// requires all inputs (including potential extenders) to apply this to their data sources in order to
    /// work correctly.
    /// </para>
    /// </remarks>
    public static ISourceBlock<IProjectVersionedValue<T>> TransformSource<T>(ISourceBlock<IProjectVersionedValue<T>> source, DisposableBag disposables, string nameFormat)
    {
        var transform = DataflowBlockSlim.CreateTransformBlock(
            static (IProjectVersionedValue<T> o) => o,
            nameFormat: nameFormat);

        disposables.Add(source.LinkTo(new FilterBlock<T>(transform), DataflowOption.PropagateCompletion));

        return transform;
    }

    private sealed class FilterBlock<T> : ITargetBlock<IProjectVersionedValue<T>>
    {
        private readonly ITargetBlock<IProjectVersionedValue<T>> _target;

        public FilterBlock(ITargetBlock<IProjectVersionedValue<T>> target) => _target = target;

        Task IDataflowBlock.Completion => _target.Completion;

        void IDataflowBlock.Complete()
        {
            // We don't want to propagate completion (although we do propagate faults).
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _target.Fault(exception);
        }

        DataflowMessageStatus ITargetBlock<IProjectVersionedValue<T>>.OfferMessage(DataflowMessageHeader messageHeader, IProjectVersionedValue<T> messageValue, ISourceBlock<IProjectVersionedValue<T>>? source, bool consumeToAccept)
        {
            var filteredValue = new ProjectVersionedValue<T>(
                messageValue.Value,
                messageValue.DataSourceVersions
                    .Remove(ProjectDataSources.ConfiguredProjectIdentity)
                    .Remove(ProjectDataSources.ConfiguredProjectVersion));

            return _target.OfferMessage(messageHeader, filteredValue, source, consumeToAccept);
        }
    }
}
