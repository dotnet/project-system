// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsChangeTracker : ProjectValueDataSourceBase<DesignTimeInputsDelta>, IDesignTimeInputsChangeTracker
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IDesignTimeInputsDataSource _inputsDataSource;
        private readonly IDesignTimeInputsFileWatcher _fileWatcher;

        private readonly DisposableBag _disposables = new DisposableBag();

        private ITargetBlock<IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>? _inputsActionBlock;
        private ITargetBlock<IProjectVersionedValue<string[]>>? _fileWatcherActionBlock;

        private DesignTimeInputs? _latestDesignTimeInputs;
        private string? _latestOutputPath;
        private int _version;

        private IBroadcastBlock<IProjectVersionedValue<DesignTimeInputsDelta>>? _broadcastBlock;

        /// <summary>
        /// The public facade for the broadcast block. We don't expose the broadcast block directly because we don't want to allow consumers to complete or fault us
        /// </summary>
        private IReceivableSourceBlock<IProjectVersionedValue<DesignTimeInputsDelta>>? _publicBlock;

        [ImportingConstructor]
        public DesignTimeInputsChangeTracker(UnconfiguredProject project,
                                             IUnconfiguredProjectServices unconfiguredProjectServices,
                                             IProjectThreadingService threadingService,
                                             IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
                                             IDesignTimeInputsDataSource inputsDataSource,
                                             IDesignTimeInputsFileWatcher fileWatcher)
            : base(unconfiguredProjectServices, synchronousDisposal: true, registerDataSource: false)
        {
            _project = project;
            _threadingService = threadingService;
            _projectSubscriptionService = projectSubscriptionService;
            _inputsDataSource = inputsDataSource;
            _fileWatcher = fileWatcher;
        }

        /// <summary>
        /// This is to allow unit tests to force completion of our source block rather than waiting for async work to complete
        /// </summary>
        internal bool AllowSourceBlockCompletion { get; set; }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(DesignTimeInputsChangeTracker));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<IProjectVersionedValue<DesignTimeInputsDelta>> SourceBlock
        {
            get
            {
                EnsureInitialized();

                return _publicBlock!;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Create an action block to process the design time inputs and configuration general changes
            _inputsActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>(ProcessDataflowChanges);

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<DesignTimeInputsDelta>>(nameFormat: nameof(DesignTimeInputsChangeTracker) + "Broadcast {1}");
            _publicBlock = AllowSourceBlockCompletion ? _broadcastBlock : _broadcastBlock.SafePublicize();

            IDisposable projectLink = ProjectDataSources.SyncLinkTo(
                   _inputsDataSource.SourceBlock.SyncLinkOptions(
                       linkOptions: DataflowOption.PropagateCompletion),
                   _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(
                      linkOptions: DataflowOption.WithRuleNames(ConfigurationGeneral.SchemaName)),
                   _inputsActionBlock,
                   DataflowOption.PropagateCompletion,
                   cancellationToken: _project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);

            // Create an action block to process file change notifications
            _fileWatcherActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<string[]>>(ProcessFileChangeNotification);
            IDisposable watcherLink = _fileWatcher.SourceBlock.LinkTo(_fileWatcherActionBlock, DataflowOption.PropagateCompletion);

            _disposables.AddDisposable(projectLink);
            _disposables.AddDisposable(watcherLink);

            JoinUpstreamDataSources(_inputsDataSource, _projectSubscriptionService.ProjectRuleSource, _fileWatcher);
        }

        protected override void Dispose(bool disposing)
        {
            if (_inputsActionBlock != null)
            {
                // This will stop our blocks taking any more input
                _inputsActionBlock.Complete();
                _fileWatcherActionBlock!.Complete();

                _threadingService.ExecuteSynchronously(() => Task.WhenAll(_inputsActionBlock.Completion, _fileWatcherActionBlock.Completion));
            }

            _disposables.Dispose();
        }

        internal void ProcessFileChangeNotification(IProjectVersionedValue<string[]> arg)
        {
            // Ignore any file changes until we've received the first set of design time inputs (which shouldn't happen anyway)
            // That first update will send out all of the files so we're not losing anything
            if (_latestDesignTimeInputs == null)
            {
                return;
            }

            // Make sure the design time inputs don't change while we're processing this notification
            DesignTimeInputs designTimeInputs = _latestDesignTimeInputs;

            var changedFiles = new List<DesignTimeInputFileChange>();
            foreach (string changedFile in arg.Value)
            {
                string relativeFilePath = _project.MakeRelative(changedFile);

                // if a shared input changes, we recompile everything
                if (designTimeInputs.SharedInputs.Contains(relativeFilePath))
                {
                    foreach (string file in designTimeInputs.Inputs)
                    {
                        changedFiles.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime: false));
                    }
                    // Since we've just queued every file, we don't care about any other changed files in this set
                    break;
                }
                else
                {
                    changedFiles.Add(new DesignTimeInputFileChange(relativeFilePath, ignoreFileWriteTime: false));
                }
            }

            PostToOutput(changedFiles);
        }

        internal void ProcessDataflowChanges(IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>> input)
        {
            DesignTimeInputs inputs = input.Value.Item1;
            IProjectChangeDescription configChanges = input.Value.Item2.ProjectChanges[ConfigurationGeneral.SchemaName];

            // This is the only method that changes _latestDesignTimeInputs, and dataflow will ensure that no calls overlap, so we're free to use it directly.
            // The same is true for _latestOutputPath

            var changedFiles = new List<DesignTimeInputFileChange>();
            // On the first call where we receive design time inputs we queue compilation of all of them, knowing that we'll only compile if the file write date requires it
            if (_latestDesignTimeInputs == null)
            {
                AddAllInputsToQueue(false);
            }
            else
            {
                // If its not the first call...

                // If a new shared design time input is added, we need to recompile everything regardless of source file modified date
                // because it could be an old file that is being promoted to a shared input
                if (inputs.SharedInputs.Except(_latestDesignTimeInputs.SharedInputs, StringComparers.Paths).Any())
                {
                    AddAllInputsToQueue(true);
                }
                // If the namespace or output path inputs have changed, then we recompile every file regardless of date
                else if (configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.RootNamespaceProperty) ||
                         configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.ProjectDirProperty) ||
                         configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.IntermediateOutputPathProperty))
                {
                    AddAllInputsToQueue(true);
                }
                else
                {
                    // Otherwise we just queue any new design time inputs, and still do date checks
                    foreach (string file in inputs.Inputs.Except(_latestDesignTimeInputs.Inputs, StringComparers.Paths))
                    {
                        changedFiles.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime: false));
                    }
                }
            }

            // Make sure we have the up to date output path
            string basePath = configChanges.After.Properties[ConfigurationGeneral.ProjectDirProperty];
            string objPath = configChanges.After.Properties[ConfigurationGeneral.IntermediateOutputPathProperty];
            try
            {
                _latestOutputPath = Path.Combine(basePath, objPath, "TempPE");
            }
            catch (ArgumentException)
            {
                // if the path is bad, then we presume we wouldn't be able to act on any files anyway
                // so we can just clear _latestDesignTimeInputs to ensure file changes aren't processed, and return.
                // If the path is ever fixed this block will trigger again and all will be right with the world.
                _latestDesignTimeInputs = null;
                return;
            }

            // Make sure we have the up to date list of inputs
            _latestDesignTimeInputs = inputs;

            PostToOutput(changedFiles);

            void AddAllInputsToQueue(bool ignoreFileWriteTime)
            {
                foreach (string file in inputs.Inputs)
                {
                    changedFiles.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime));
                }
            }
        }

        private void PostToOutput(List<DesignTimeInputFileChange> changedFiles)
        {
            Assumes.NotNull(_latestDesignTimeInputs);

            // Nothing calls this method without setting _latestDesignTimeInputs
            var delta = new DesignTimeInputsDelta(_latestDesignTimeInputs!.Inputs, _latestDesignTimeInputs.SharedInputs, changedFiles, _latestOutputPath!);

            _version++;
            ImmutableDictionary<NamedIdentity, IComparable> dataSources = ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(DataSourceKey, DataSourceVersion);

            _broadcastBlock.Post(new ProjectVersionedValue<DesignTimeInputsDelta>(delta, dataSources));
        }
    }
}
