using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IFileWatchProviderDataSource))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class PotentialEditorConfigDataSource : ProjectValueDataSourceBase<IFileWatchProvider>, IFileWatchProviderDataSource
    {
        private readonly ConfiguredProject _project;

        private int _version;
        private DisposableBag _disposables;

        /// <summary>
        /// The block that receives updates from the active tree provider.
        /// </summary>
        private IBroadcastBlock<IProjectVersionedValue<IFileWatchProvider>> _broadcastBlock;

        /// <summary>
        /// The public facade for the broadcast block.
        /// </summary>
        private IReceivableSourceBlock<IProjectVersionedValue<IFileWatchProvider>> _publicBlock;

        [ImportingConstructor]
        public PotentialEditorConfigDataSource(ConfiguredProject project)
            : base(project.Services, synchronousDisposal: true)
        {
            _project = project;
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(PotentialEditorConfigDataSource));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<IProjectVersionedValue<IFileWatchProvider>> SourceBlock
        {
            get
            {
                EnsureInitialized();

                return _publicBlock;
            }
        }

        public FileChangeKinds ChangeKinds => FileChangeKinds.Added | FileChangeKinds.Removed;

        protected override void Initialize()
        {
            base.Initialize();

            IDisposable projectRuleSourceLink = _project.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.LinkToAsyncAction(ProcessProjectChanged, ruleNames: PotentialEditorConfigFiles.SchemaName);

            IDisposable join = JoinUpstreamDataSources(_project.Services.ProjectSubscription.ProjectRuleSource);

            _disposables = new DisposableBag();
            _disposables.AddDisposable(projectRuleSourceLink);
            _disposables.AddDisposable(join);

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<IFileWatchProvider>>(nameFormat: nameof(PotentialEditorConfigDataSource) + "Broadcast {1}");
            _publicBlock = _broadcastBlock.SafePublicize();
        }

        private async Task ProcessProjectChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> projectSnapshot)
        {
            if (projectSnapshot.Value.CurrentState.TryGetValue(PotentialEditorConfigFiles.SchemaName, out IProjectRuleSnapshot ruleSnapshot))
            {
                var fileWatchProvider = new PotentialEditorConfigFileWatchProvider(this, ruleSnapshot.Items.Keys);
                _version++;
                ImmutableDictionary<NamedIdentity, IComparable> dataSources = ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(DataSourceKey, DataSourceVersion);

                await _broadcastBlock.SendAsync(new ProjectVersionedValue<IFileWatchProvider>(fileWatchProvider, dataSources));
            }
        }

        private class PotentialEditorConfigFileWatchProvider : IFileWatchProvider
        {
            public PotentialEditorConfigFileWatchProvider(
                IFileWatchProviderDataSource dataSource,
                IEnumerable<string> filePathsToWatch)
            {
                DataSource = dataSource;
                FilePathsToWatch = filePathsToWatch;
            }

            public IFileWatchProviderDataSource DataSource { get; }
            public IEnumerable<string> FilePathsToWatch { get; }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsInitialized)
            {
                _disposables.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
