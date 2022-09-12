// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(IDesignTimeInputsCompiler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal partial class DesignTimeInputsCompiler : OnceInitializedOnceDisposedAsync, IDesignTimeInputsCompiler
    {
        private static readonly TimeSpan s_compilationDelayTime = TimeSpan.FromMilliseconds(500);

        private readonly UnconfiguredProject _project;
        private readonly IWorkspaceWriter _workspaceWriter;
        private readonly IProjectThreadingService _threadingService;
        private readonly IDesignTimeInputsChangeTracker _changeTracker;
        private readonly ITempPECompiler _compiler;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;
        private readonly TaskDelayScheduler _scheduler;

        private ITargetBlock<IProjectVersionedValue<DesignTimeInputSnapshot>>? _compileActionBlock;
        private IDisposable? _changeTrackerLink;

        private readonly CompilationQueue _queue = new();
        private CancellationTokenSource? _compilationCancellationSource;

        [ImportingConstructor]
        public DesignTimeInputsCompiler(UnconfiguredProject project,
                                        IWorkspaceWriter workspaceWriter,
                                        IProjectThreadingService threadingService,
                                        IDesignTimeInputsChangeTracker changeTracker,
                                        ITempPECompiler compiler,
                                        IFileSystem fileSystem,
                                        ITelemetryService telemetryService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _workspaceWriter = workspaceWriter;
            _threadingService = threadingService;
            _changeTracker = changeTracker;
            _compiler = compiler;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
            _scheduler = new TaskDelayScheduler(s_compilationDelayTime, threadingService, CancellationToken.None);
        }

        /// <summary>
        /// This is to allow unit tests to run the compilation synchronously rather than waiting for async work to complete
        /// </summary>
        internal bool CompileSynchronously { get; set; }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _compileActionBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<DesignTimeInputSnapshot>>(ProcessDataflowChanges, _project);

            _changeTrackerLink = _changeTracker.SourceBlock.LinkTo(_compileActionBlock, DataflowOption.PropagateCompletion);

            return Task.CompletedTask;
        }

        internal void ProcessDataflowChanges(IProjectVersionedValue<DesignTimeInputSnapshot> value)
        {
            // Cancel any in-progress queue processing
            _compilationCancellationSource?.Cancel();

            DesignTimeInputSnapshot snapshot = value.Value;

            // add all of the changes to our queue
            _queue.Update(snapshot.ChangedInputs, snapshot.Inputs, snapshot.SharedInputs, snapshot.TempPEOutputPath);

            Assumes.Present(_project.Services.ProjectAsynchronousTasks);

            // Create a cancellation source so we can cancel the compilation if another message comes through
            _compilationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);

            JoinableTask task = _scheduler.ScheduleAsyncTask(ProcessCompileQueueAsync, _compilationCancellationSource.Token);

            // For unit testing purposes, optionally block the thread until the task we scheduled is complete
            if (CompileSynchronously)
            {
                _threadingService.ExecuteSynchronously(() => task.Task);
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (_compileActionBlock is not null)
            {
                // This will stop our blocks taking any more input
                _compileActionBlock.Complete();

                await _compileActionBlock.Completion;
            }

            _compilationCancellationSource?.Dispose();
            _scheduler.Dispose();
            _changeTrackerLink?.Dispose();
        }

        private Task ProcessCompileQueueAsync(CancellationToken token)
        {
            int compileCount = 0;
            int initialQueueLength = _queue.Count;
            var compileStopWatch = Stopwatch.StartNew();
            return _workspaceWriter.WriteAsync(async workspace =>
            {
                while (true)
                {
                    if (IsDisposing || IsDisposed)
                    {
                        return;
                    }

                    // we don't want to pop if we've been cancelled in the time it took to take the write lock, so check just in case.
                    // this may be overkill
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    // Grab the next file to compile off the queue
                    QueueItem? item = _queue.Pop();
                    if (item is null)
                    {
                        break;
                    }

                    bool cancelled = false;
                    string outputFileName = GetOutputFileName(item.FileName, item.TempPEOutputPath);
                    try
                    {
                        if (await CompileDesignTimeInputAsync(workspace.Context, item.FileName, outputFileName, item.SharedInputs, item.IgnoreFileWriteTime, token))
                        {
                            compileCount++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                    }

                    if (cancelled || token.IsCancellationRequested)
                    {
                        // if the compilation was cancelled, we need to re-add the file so we catch it next time
                        _queue.Push(item);
                        break;
                    }
                }

                LogTelemetry(cancelled: token.IsCancellationRequested);
            },
            token);

            void LogTelemetry(bool cancelled)
            {
                compileStopWatch!.Stop();
                _telemetryService.PostProperties(TelemetryEventName.TempPEProcessQueue, new[]
                {
                    ( TelemetryPropertyName.TempPE.CompileCount,        (object)compileCount),
                    ( TelemetryPropertyName.TempPE.InitialQueueLength,  initialQueueLength),
                    ( TelemetryPropertyName.TempPE.CompileWasCancelled, cancelled),
                    ( TelemetryPropertyName.TempPE.CompileDuration,     compileStopWatch.ElapsedMilliseconds)
                });
            }
        }

        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        /// <param name="relativeFileName">A project relative path to a source file that is a design time input</param>
        /// <param name="tempPEOutputPath">The path in which to place the TempPE DLL if one is created</param>
        /// <param name="sharedInputs">The list of shared inputs to be included in the TempPE DLL</param>
        /// <returns>An XML description of the TempPE DLL for the specified file</returns>
        public async Task<string> BuildDesignTimeOutputAsync(string relativeFileName, string tempPEOutputPath, ImmutableHashSet<string> sharedInputs)
        {
            // A call to this method indicates that the TempPE system is in use for real, so we use it as a trigger for starting background compilation of things
            // This means we get a nicer experience for the user once they start using designers, without wasted cycles compiling things just because a project is loaded
            await InitializeAsync();

            int initialQueueLength = _queue.Count;

            string fileName = _project.MakeRooted(relativeFileName);

            // Remove the file from our todo list, in case it was in there.
            // Note that other than this avoidance of unnecessary work, this method is stateless.
            _queue.RemoveSpecific(fileName);

            string outputFileName = GetOutputFileNameFromRelativePath(relativeFileName, tempPEOutputPath);
            // make sure the file is up to date
            bool compiled = await _workspaceWriter.WriteAsync(workspace =>
            {
                return CompileDesignTimeInputAsync(workspace.Context, fileName, outputFileName, sharedInputs, ignoreFileWriteTime: false);
            });

            if (compiled)
            {
                _telemetryService.PostProperties(TelemetryEventName.TempPECompileOnDemand, new[]
                {
                    ( TelemetryPropertyName.TempPE.InitialQueueLength, (object)initialQueueLength)
                });
            }

            return $@"<root>
  <Application private_binpath = ""{Path.GetDirectoryName(outputFileName)}""/>
  <Assembly
    codebase = ""{Path.GetFileName(outputFileName)}""
    name = ""{relativeFileName}""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>";
        }

        private async Task<bool> CompileDesignTimeInputAsync(IWorkspaceProjectContext context, string designTimeInput, string outputFileName, ImmutableHashSet<string> sharedInputs, bool ignoreFileWriteTime, CancellationToken token = default)
        {
            HashSet<string> filesToCompile = GetFilesToCompile(designTimeInput, sharedInputs);

            if (token.IsCancellationRequested)
            {
                return false;
            }

            if (ignoreFileWriteTime || CompilationNeeded(filesToCompile, outputFileName))
            {
                bool result = false;
                try
                {
                    result = await _compiler.CompileAsync(context, outputFileName, filesToCompile, token);
                }
                catch (IOException)
                { }
                finally
                {
                    // If the compilation failed or was cancelled we should clean up any old TempPE outputs lest a designer gets the wrong types, plus its what legacy did
                    // plus the way the Roslyn compiler works is by creating a 0 byte file first
                    if (!result)
                    {
                        try
                        {
                            _fileSystem.RemoveFile(outputFileName);
                        }
                        catch (IOException)
                        { }
                        catch (UnauthorizedAccessException)
                        { }
                    }
                }

                // true in this case means "we tried to compile", and is just for telemetry reasons. It doesn't indicate success or failure of compilation
                return true;
            }
            return false;
        }

        private string GetOutputFileName(string fileName, string tempPEOutputPath)
        {
            // Turn the file path back into a relative path to compute output DLL name
            return GetOutputFileNameFromRelativePath(_project.MakeRelative(fileName), tempPEOutputPath);
        }

        private static string GetOutputFileNameFromRelativePath(string relativePath, string tempPEOutputPath)
        {
            // Since we are given a relative path we can just replace path separators and we know we'll have a valid filename
            return Path.Combine(tempPEOutputPath, relativePath.Replace('\\', '.') + ".dll");
        }

        private bool CompilationNeeded(HashSet<string> files, string outputFileName)
        {
            if (!_fileSystem.TryGetLastFileWriteTimeUtc(outputFileName, out DateTime? outputDateTime))
                return true; // File does not exist

            foreach (string file in files)
            {
                DateTime fileDateTime = _fileSystem.GetLastFileWriteTimeOrMinValueUtc(file);
                if (fileDateTime > outputDateTime)
                    return true;
            }

            return false;
        }

        private static HashSet<string> GetFilesToCompile(string moniker, ImmutableHashSet<string> sharedInputs)
        {
            // This is a HashSet because we allow files to be both inputs and shared inputs, and we don't want to compile the same file twice,
            // plus Roslyn needs to call Contains on this quite a lot in order to ensure its only compiling the right files so we want that to be fast.
            // When it comes to compiling the files there is no difference between shared and normal design time inputs, we just track differently because
            // shared are included in every DLL.
            var files = new HashSet<string>(sharedInputs.Count + 1, StringComparers.Paths);
            files.AddRange(sharedInputs);
            files.Add(moniker);
            return files;
        }
    }
}
