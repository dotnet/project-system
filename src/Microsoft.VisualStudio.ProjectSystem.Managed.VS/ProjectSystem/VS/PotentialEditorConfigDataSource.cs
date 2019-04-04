// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IFileWatchDataSource))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class PotentialEditorConfigDataSource : ProjectValueDataSourceBase<FileWatchData>, IFileWatchDataSource
    {
        private readonly ConfiguredProject _project;

        private int _version;
        private DisposableBag _disposables;

        /// <summary>
        /// The block that receives updates from the active tree provider.
        /// </summary>
        private IBroadcastBlock<IProjectVersionedValue<FileWatchData>> _broadcastBlock;

        /// <summary>
        /// The public facade for the broadcast block.
        /// </summary>
        private IReceivableSourceBlock<IProjectVersionedValue<FileWatchData>> _publicBlock;

        [ImportingConstructor]
        public PotentialEditorConfigDataSource(ConfiguredProject project)
            : base(project.Services, synchronousDisposal: true)
        {
            _project = project;
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(PotentialEditorConfigDataSource));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<IProjectVersionedValue<FileWatchData>> SourceBlock
        {
            get
            {
                EnsureInitialized();

                return _publicBlock;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            IDisposable projectRuleSourceLink = _project.Services.ProjectSubscription.ProjectRuleSource.SourceBlock.LinkToAsyncAction(ProcessProjectChanged, ruleNames: PotentialEditorConfigFiles.SchemaName);

            IDisposable join = JoinUpstreamDataSources(_project.Services.ProjectSubscription.ProjectRuleSource);

            _disposables = new DisposableBag();
            _disposables.AddDisposable(projectRuleSourceLink);
            _disposables.AddDisposable(join);

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<FileWatchData>>(nameFormat: nameof(PotentialEditorConfigDataSource) + "Broadcast {1}");
            _publicBlock = _broadcastBlock.SafePublicize();
        }

        private async Task ProcessProjectChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> projectSnapshot)
        {
            if (projectSnapshot.Value.CurrentState.TryGetValue(PotentialEditorConfigFiles.SchemaName, out IProjectRuleSnapshot ruleSnapshot))
            {
                var builder = ImmutableList.CreateBuilder<string>();
                foreach (var item in ruleSnapshot.Items)
                {
                    IImmutableDictionary<string, string> metadata = item.Value;
                    string fileFullPath = metadata.TryGetValue("DefiningProjectDirectory", out string definingProjectDirectory)
                        ? MakeRooted(definingProjectDirectory, item.Key)
                        : _project.UnconfiguredProject.MakeRooted(item.Key);

                    builder.Add(fileFullPath);
                }

                var fileWatchProvider = new FileWatchData(this, builder.ToImmutable(), FileWatchChangeKinds.Added | FileWatchChangeKinds.Removed);
                _version++;
                ImmutableDictionary<NamedIdentity, IComparable> dataSources = ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(DataSourceKey, DataSourceVersion);

                await _broadcastBlock.SendAsync(new ProjectVersionedValue<FileWatchData>(fileWatchProvider, dataSources));
            }
        }

        private static string MakeRooted(string basePath, string path)
        {
            basePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return PathHelper.MakeRooted(basePath + Path.DirectorySeparatorChar, path);
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
