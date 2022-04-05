// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<T>>(options: null);
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
            await _broadcastBlock!.SendAsync(new ProjectVersionedValue<T>(
                     value,
                     ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(DataSourceKey, _version)));
        }
    }
}
