// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     A <see cref="IProjectValueDataSource{T}"/> that drops <see cref="ProjectDataSources.ConfiguredProjectIdentity"/> and
    ///     <see cref="ProjectDataSources.ConfiguredProjectVersion"/> versions from each value of the original
    ///     <see cref="IProjectValueDataSource{T}"/>.
    /// </summary>
    internal sealed class DropConfiguredProjectVersionDataSource<T> : ChainedProjectValueDataSourceBase<T>
        where T : class
    {
        private readonly IProjectValueDataSource<T> _dataSource;

        public DropConfiguredProjectVersionDataSource(UnconfiguredProject project, IProjectValueDataSource<T> dataSource)
            : base(project, synchronousDisposal: false, registerDataSource: false)
        {
            _dataSource = dataSource;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<T>> targetBlock)
        {
            JoinUpstreamDataSources(_dataSource);

            DisposableValue<ISourceBlock<IProjectVersionedValue<T>>> block = _dataSource.SourceBlock.Transform(DropConfiguredProjectVersion);

            block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return block;
        }

        private IProjectVersionedValue<T> DropConfiguredProjectVersion(IProjectVersionedValue<T> data)
        {
            return new ProjectVersionedValue<T>(data.Value, data.DataSourceVersions.Remove(ProjectDataSources.ConfiguredProjectIdentity)
                                                                                   .Remove(ProjectDataSources.ConfiguredProjectVersion));
        }
    }
}
