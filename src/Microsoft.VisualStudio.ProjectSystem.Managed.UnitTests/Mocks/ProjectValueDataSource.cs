// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class ProjectValueDataSource<T> : ProjectValueDataSourceBase<T>
        where T : class
    {
        private IBroadcastBlock<IProjectVersionedValue<T>>? _broadcastBlock;
        private int _version;

        public ProjectValueDataSource(IProjectCommonServices services)
            : base(services)
        {
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity("DataSource");

        public override IComparable DataSourceVersion
        {
            get { return _version; }
        }

        public override IReceivableSourceBlock<IProjectVersionedValue<T>> SourceBlock
        {
            get
            {
                EnsureInitialized(true);

                return _broadcastBlock!;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<T>>(null);
        }

        public async Task SendAndCompleteAsync(T value, IDataflowBlock targetBlock)
        {
            await SendAsync(value);

            _broadcastBlock!.Complete();

            // Note, we have to wait for both the source *and* target block as 
            // the Completion of the source block doesn't mean that the target 
            // block has finished.
            await Task.WhenAll(_broadcastBlock.Completion, targetBlock.Completion);
        }

        public async Task SendAsync(T value)
        {
            EnsureInitialized(true);

            _version++;
            await _broadcastBlock.SendAsync(new ProjectVersionedValue<T>(
                     value,
                     ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(DataSourceKey, _version)));
        }
    }
}
