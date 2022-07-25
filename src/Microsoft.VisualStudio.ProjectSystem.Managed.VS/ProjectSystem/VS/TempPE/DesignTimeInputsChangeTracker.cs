// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(IDesignTimeInputsChangeTracker))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class DesignTimeInputsChangeTracker : ProjectValueDataSourceBase<DesignTimeInputSnapshot>, IDesignTimeInputsChangeTracker
    {
        private readonly UnconfiguredProject _project;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IDesignTimeInputsDataSource _inputsDataSource;
        private readonly IDesignTimeInputsFileWatcher _fileWatcher;

        private readonly DisposableBag _disposables = new();

        private DesignTimeInputSnapshot? _currentState;
        private int _version;

        private IBroadcastBlock<IProjectVersionedValue<DesignTimeInputSnapshot>>? _broadcastBlock;

        /// <summary>
        /// The public facade for the broadcast block. We don't expose the broadcast block directly because we don't want to allow consumers to complete or fault us
        /// </summary>
        private IReceivableSourceBlock<IProjectVersionedValue<DesignTimeInputSnapshot>>? _publicBlock;

        [ImportingConstructor]
        public DesignTimeInputsChangeTracker(UnconfiguredProject project,
                                             IUnconfiguredProjectServices unconfiguredProjectServices,
                                             IProjectThreadingService threadingService,
                                             IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
                                             IDesignTimeInputsDataSource inputsDataSource,
                                             IDesignTimeInputsFileWatcher fileWatcher)
            : base(unconfiguredProjectServices, synchronousDisposal: false, registerDataSource: false)
        {
            _project = project;
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

        public override IReceivableSourceBlock<IProjectVersionedValue<DesignTimeInputSnapshot>> SourceBlock
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
            ITargetBlock<IProjectVersionedValue<ValueTuple<DesignTimeInputs, IProjectSubscriptionUpdate>>> inputsAction = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<ValueTuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>(ProcessDataflowChanges, _project);

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<DesignTimeInputSnapshot>>(nameFormat: nameof(DesignTimeInputsChangeTracker) + " Broadcast: {1}");
            _publicBlock = AllowSourceBlockCompletion ? _broadcastBlock : _broadcastBlock.SafePublicize();

            Assumes.Present(_project.Services.ProjectAsynchronousTasks);

            IDisposable projectLink = ProjectDataSources.SyncLinkTo(
                   _inputsDataSource.SourceBlock.SyncLinkOptions(
                       linkOptions: DataflowOption.PropagateCompletion),
                   _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(
                      linkOptions: DataflowOption.WithRuleNames(ConfigurationGeneral.SchemaName)),
                   inputsAction,
                   DataflowOption.PropagateCompletion,
                   cancellationToken: _project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);

            // Create an action block to process file change notifications
            ITargetBlock<IProjectVersionedValue<string[]>> fileWatcherAction = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<string[]>>(ProcessFileChangeNotification, _project);
            IDisposable watcherLink = _fileWatcher.SourceBlock.LinkTo(fileWatcherAction, DataflowOption.PropagateCompletion);

            _disposables.Add(projectLink);
            _disposables.Add(watcherLink);

            JoinUpstreamDataSources(_inputsDataSource, _projectSubscriptionService.ProjectRuleSource, _fileWatcher);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _disposables.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal void ProcessFileChangeNotification(IProjectVersionedValue<string[]> arg)
        {
            // File changes don't change state, but it makes sense to run with the state at the time the update came in
            DesignTimeInputSnapshot? state = _currentState;

            // Ignore any file changes until we've received the first set of design time inputs (which shouldn't happen anyway)
            // That first update will send out all of the files so we're not losing anything
            if (state is null)
            {
                return;
            }

            var changedInputs = new List<DesignTimeInputFileChange>();
            foreach (string changedFile in arg.Value)
            {
                // if a shared input changes, we recompile everything
                if (state.SharedInputs.Contains(changedFile))
                {
                    foreach (string file in state.Inputs)
                    {
                        changedInputs.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime: false));
                    }
                    // Since we've just queued every file, we don't care about any other changed files in this set
                    break;
                }
                else
                {
                    changedInputs.Add(new DesignTimeInputFileChange(changedFile, ignoreFileWriteTime: false));
                }
            }

            // File changes don't get project state, so they don't update it.
            var snapshot = new DesignTimeInputSnapshot(state.Inputs, state.SharedInputs, changedInputs, state.TempPEOutputPath);
            PublishSnapshot(snapshot);
        }

        // Should always produce output data to avoid hangs.
        internal void ProcessDataflowChanges(IProjectVersionedValue<ValueTuple<DesignTimeInputs, IProjectSubscriptionUpdate>> input)
        {
            _currentState = GenerateOutputData(_currentState, input) ?? DesignTimeInputSnapshot.Empty;

            PublishSnapshot(_currentState);
        }

        private static DesignTimeInputSnapshot? GenerateOutputData(
            DesignTimeInputSnapshot? previousState,
            IProjectVersionedValue<ValueTuple<DesignTimeInputs, IProjectSubscriptionUpdate>> input)
        {
            DesignTimeInputs inputs = input.Value.Item1;

            if (!input.Value.Item2.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectChangeDescription configChanges))
            {
                // If this isn't an update we can deal with, just ignore it
                return null;
            }

            var changedInputs = new List<DesignTimeInputFileChange>();
            // On the first call where we receive design time inputs we queue compilation of all of them, knowing that we'll only compile if the file write date requires it
            if (previousState is null)
            {
                AddAllInputsToQueue(false);
            }
            else
            {
                // If its not the first call...

                // If a new shared design time input is added, we need to recompile everything regardless of source file modified date
                // because it could be an old file that is being promoted to a shared input
                if (inputs.SharedInputs.Except(previousState.SharedInputs, StringComparers.Paths).Any())
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
                    foreach (string file in inputs.Inputs.Except(previousState.Inputs, StringComparers.Paths))
                    {
                        changedInputs.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime: false));
                    }
                }
            }

            string tempPEOutputPath;
            // Make sure we have the up to date output path. If either of these don't exist, they will be null and we'll handle the ArgumentException below
            string? basePath = configChanges.After.Properties.GetValueOrDefault(ConfigurationGeneral.ProjectDirProperty);
            string? objPath = configChanges.After.Properties.GetValueOrDefault(ConfigurationGeneral.IntermediateOutputPathProperty);
            try
            {
                tempPEOutputPath = Path.Combine(basePath, objPath, "TempPE");
            }
            catch (ArgumentException)
            {
                // if the path is bad, or we couldn't get part of it, then we presume we wouldn't be able to act on any files
                // so we can just clear _currentState to ensure file changes aren't processed, and return.
                // If the path is ever fixed this block will trigger again and all will be right with the world.
                return null;
            }

            // This is our only update to current state, and data flow protects us from overlaps. File changes don't update state
            return new DesignTimeInputSnapshot(inputs.Inputs, inputs.SharedInputs, changedInputs, tempPEOutputPath);

            void AddAllInputsToQueue(bool ignoreFileWriteTime)
            {
                foreach (string file in inputs.Inputs)
                {
                    changedInputs.Add(new DesignTimeInputFileChange(file, ignoreFileWriteTime));
                }
            }
        }

        private void PublishSnapshot(DesignTimeInputSnapshot snapshot)
        {
            _version++;
            _broadcastBlock?.Post(new ProjectVersionedValue<DesignTimeInputSnapshot>(
                snapshot,
                Empty.ProjectValueVersions.Add(DataSourceKey, _version)));
        }
    }
}
