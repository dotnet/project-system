// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal partial class PackageRestoreUnconfiguredInputDataSource
    {
        /// <summary>
        ///     A <see cref="IProjectValueDataSource{T}"/> that drops <see cref="ProjectDataSources.ConfiguredProjectIdentity"/> and 
        ///     <see cref="ProjectDataSources.ConfiguredProjectVersion"/> versions from each value of the original
        ///     <see cref="IProjectValueDataSource{T}"/>.
        /// </summary>
        private sealed class DropConfiguredProjectVersionDataSource<T> : ChainedProjectValueDataSourceBase<T>
            where T : class
        {
            private readonly IProjectValueDataSource<T> _dataSource;

            public DropConfiguredProjectVersionDataSource(IProjectServices commonServices, IProjectValueDataSource<T> dataSource)
                : base(commonServices, synchronousDisposal: true, registerDataSource: false)
            {
                _dataSource = dataSource;
            }

            protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<T>> targetBlock)
            {
                DisposableValue<ISourceBlock<IProjectVersionedValue<T>>> block = _dataSource.SourceBlock.Transform(DropConfiguredProjectVersion);

                block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

                JoinUpstreamDataSources(_dataSource);

                return block;
            }

            private static IProjectVersionedValue<T> DropConfiguredProjectVersion(IProjectVersionedValue<T> data)
            {
                return new ProjectVersionedValue<T>(data.Value, data.DataSourceVersions.Remove(ProjectDataSources.ConfiguredProjectIdentity)
                                                                                       .Remove(ProjectDataSources.ConfiguredProjectVersion));
            }
        }
    }
}
