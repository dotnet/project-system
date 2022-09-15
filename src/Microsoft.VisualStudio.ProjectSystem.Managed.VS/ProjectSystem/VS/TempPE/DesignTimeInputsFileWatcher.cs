// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// Produces output whenever a design time input changes
    /// </summary>
    [Export(typeof(IDesignTimeInputsFileWatcher))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class DesignTimeInputsFileWatcher : ProjectValueDataSourceBase<string[]>, IVsFreeThreadedFileChangeEvents2, IDesignTimeInputsFileWatcher
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IDesignTimeInputsDataSource _designTimeInputsDataSource;
        private readonly IVsService<IVsAsyncFileChangeEx> _fileChangeService;
        private readonly Dictionary<string, uint> _fileWatcherCookies = new(StringComparers.Paths);

        private int _version;
        private IDisposable? _dataSourceLink;

        /// <summary>
        /// The block that receives updates from the active tree provider.
        /// </summary>
        private IBroadcastBlock<IProjectVersionedValue<string[]>>? _broadcastBlock;

        /// <summary>
        /// The public facade for the broadcast block.
        /// </summary>
        private IReceivableSourceBlock<IProjectVersionedValue<string[]>>? _publicBlock;

        /// <summary>
        /// The block that actually does our processing
        /// </summary>
        private ITargetBlock<IProjectVersionedValue<DesignTimeInputs>>? _actionBlock;

        [ImportingConstructor]
        public DesignTimeInputsFileWatcher(UnconfiguredProject project,
                                           IUnconfiguredProjectServices unconfiguredProjectServices,
                                           IProjectThreadingService threadingService,
                                           IDesignTimeInputsDataSource designTimeInputsDataSource,
                                           IVsService<SVsFileChangeEx, IVsAsyncFileChangeEx> fileChangeService)
             : base(unconfiguredProjectServices, synchronousDisposal: false, registerDataSource: false)
        {
            _project = project;
            _threadingService = threadingService;
            _designTimeInputsDataSource = designTimeInputsDataSource;
            _fileChangeService = fileChangeService;
        }

        /// <summary>
        /// This is to allow unit tests to force completion of our source block rather than waiting for async work to complete
        /// </summary>
        internal bool AllowSourceBlockCompletion { get; set; }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(DesignTimeInputsFileWatcher));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<IProjectVersionedValue<string[]>> SourceBlock
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

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<string[]>>(nameFormat: nameof(DesignTimeInputsFileWatcher) + " Broadcast: {1}");
            _publicBlock = AllowSourceBlockCompletion ? _broadcastBlock : _broadcastBlock.SafePublicize();

            _actionBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<DesignTimeInputs>>(ProcessDesignTimeInputsAsync, _project);

            _dataSourceLink = _designTimeInputsDataSource.SourceBlock.LinkTo(_actionBlock, DataflowOption.PropagateCompletion);

            JoinUpstreamDataSources(_designTimeInputsDataSource);
        }

        private async Task ProcessDesignTimeInputsAsync(IProjectVersionedValue<DesignTimeInputs> input)
        {
            DesignTimeInputs designTimeInputs = input.Value;

            IVsAsyncFileChangeEx vsAsyncFileChangeEx = await _fileChangeService.GetValueAsync();

            // we don't care about the difference between types of inputs, so we just construct one hashset for fast comparisons later
            var allFiles = new HashSet<string>(StringComparers.Paths);
            allFiles.AddRange(designTimeInputs.Inputs);
            allFiles.AddRange(designTimeInputs.SharedInputs);

            // Remove any files we're watching that we don't care about any more
            var removedFiles = new List<string>();
            foreach ((string file, uint cookie) in _fileWatcherCookies)
            {
                if (!allFiles.Contains(file))
                {
                    await vsAsyncFileChangeEx.UnadviseFileChangeAsync(cookie);
                    removedFiles.Add(file);
                }
            }

            foreach (string file in removedFiles)
            {
                _fileWatcherCookies.Remove(file);
            }

            // Now watch and output files that are new
            foreach (string file in allFiles)
            {
                if (!_fileWatcherCookies.ContainsKey(file))
                {
                    // We don't care about delete and add here, as they come through data flow, plus they are really bouncy - every file change is a Time, Del and Add event)
                    uint cookie = await vsAsyncFileChangeEx.AdviseFileChangeAsync(file, _VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size, sink: this);

                    _fileWatcherCookies.Add(file, cookie);
                }
            }
        }

        private void PublishFiles(string[] files)
        {
            _version++;
            _broadcastBlock?.Post(new ProjectVersionedValue<string[]>(
                files,
                Empty.ProjectValueVersions.Add(DataSourceKey, _version)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Completing the output block before the action block means any final messages that are currently being produced
                // will not be sent out, which is what we want in this case.
                _broadcastBlock?.Complete();
                _dataSourceLink?.Dispose();

                if (_actionBlock is not null)
                {
                    _actionBlock.Complete();

                    _threadingService.ExecuteSynchronously(async () =>
                    {
                        // Wait for any processing to finish so we don't fight over the cookies 🍪
                        await _actionBlock.Completion;

                        IVsAsyncFileChangeEx vsAsyncFileChangeEx = await _fileChangeService.GetValueAsync();

                        // Unsubscribe from all files
                        foreach (uint cookie in _fileWatcherCookies.Values)
                        {
                            await vsAsyncFileChangeEx.UnadviseFileChangeAsync(cookie);
                        }
                    });
                }
            }

            base.Dispose(disposing);
        }

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            PublishFiles(rgpszFile);

            return HResult.OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return HResult.NotImplemented;
        }

        public int DirectoryChangedEx(string pszDirectory, string pszFile)
        {
            return HResult.NotImplemented;
        }

        public int DirectoryChangedEx2(string pszDirectory, uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            return HResult.NotImplemented;
        }
    }
}
