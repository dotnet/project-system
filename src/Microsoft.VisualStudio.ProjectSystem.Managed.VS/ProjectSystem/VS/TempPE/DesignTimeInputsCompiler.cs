// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsCompiler : OnceInitializedOnceDisposedAsync
    {
        private static readonly TimeSpan s_compilationDelayTime = TimeSpan.FromMilliseconds(500);

        private readonly UnconfiguredProject _project;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IProjectThreadingService _threadingService;
        private readonly IDesignTimeInputsChangeTracker _changeTracker;
        private readonly ITempPECompiler _compiler;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;
        private readonly TaskDelayScheduler _scheduler;

        private ITargetBlock<IProjectVersionedValue<DesignTimeInputsDelta>>? _deltaActionBlock;
        private IDisposable? _changeTrackerLink;

        private DesignTimeInputsDelta? _state;

        private ImmutableDictionary<string, bool> _filesToCompile = ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparers.Paths); // Key is filename, value is whether to ignore the last write time check
        private CancellationTokenSource? _compilationCancellationSource;

        [ImportingConstructor]
        public DesignTimeInputsCompiler(UnconfiguredProject project,
                                        IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost,
                                        IProjectThreadingService threadingService,
                                        IDesignTimeInputsChangeTracker changeTracker,
                                        ITempPECompiler compiler,
                                        IFileSystem fileSystem,
                                        ITelemetryService telemetryService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
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
            // Create an action block process the design time delta
            _deltaActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<DesignTimeInputsDelta>>(ProcessDataflowChanges);

            _changeTrackerLink = _changeTracker.SourceBlock.LinkTo(_deltaActionBlock, DataflowOption.PropagateCompletion);

            return Task.CompletedTask;
        }

        internal void ProcessDataflowChanges(IProjectVersionedValue<DesignTimeInputsDelta> obj)
        {
            // Cancel any in-progress queue processing
            _compilationCancellationSource?.Cancel();

            // Capture the latest state
            _state = obj.Value;

            // add all of the changes to our queue
            foreach (DesignTimeInputFileChange item in _state.ChangedInputs)
            {
                ImmutableInterlocked.TryAdd(ref _filesToCompile, item.File, item.IgnoreFileWriteTime);
            }

            // remove any items we have queued that aren't in the inputs any more
            foreach ((string file, _) in _filesToCompile)
            {
                if (!_state.Inputs.Contains(file))
                {
                    ImmutableInterlocked.TryRemove(ref _filesToCompile, file, out _);
                }
            }

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
            if (_deltaActionBlock != null)
            {
                // This will stop our blocks taking any more input
                _deltaActionBlock.Complete();

                await _deltaActionBlock.Completion;
            }

            _compilationCancellationSource?.Dispose();
            _scheduler.Dispose();
            _changeTrackerLink?.Dispose();
        }

        private Task ProcessCompileQueueAsync(CancellationToken token)
        {
            int compileCount = 0;
            int initialQueueLength = _filesToCompile.Count;
            var compileStopWatch = Stopwatch.StartNew();
            return _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(async accessor =>
            {
                while (_filesToCompile.Count > 0)
                {
                    if (IsDisposing || IsDisposed)
                    {
                        return;
                    }

                    if (token.IsCancellationRequested)
                    {
                        LogTelemetry(cancelled: true);
                        return;
                    }

                    // Grab the next file to compile off the queue
                    (string fileName, bool ignoreFileWriteTime) = _filesToCompile.FirstOrDefault();
                    if (fileName == null)
                    {
                        break;
                    }

                    string outputFileName = GetOutputFileName(fileName);
                    try
                    {
                        if (await CompileDesignTimeInputAsync(accessor.Context, fileName, outputFileName, ignoreFileWriteTime, token))
                        {
                            compileCount++;
                        }
                        // We remove the file if the compilation wasn't cancelled, regardless of whether it really happened or not.
                        ImmutableInterlocked.TryRemove(ref _filesToCompile, fileName, out _);
                    }
                    catch (OperationCanceledException)
                    {
                        LogTelemetry(cancelled: true);
                        return;
                    }
                }

                LogTelemetry(cancelled: false);
            });

            void LogTelemetry(bool cancelled)
            {
                compileStopWatch.Stop();
                _telemetryService.PostProperties(TelemetryEventName.TempPEProcessQueue, new[]
                {
                    ( TelemetryPropertyName.TempPECompileCount,        (object)compileCount),
                    ( TelemetryPropertyName.TempPEInitialQueueLength,  initialQueueLength),
                    ( TelemetryPropertyName.TempPECompileWasCancelled, cancelled),
                    ( TelemetryPropertyName.TempPECompileDuration,     compileStopWatch.ElapsedMilliseconds)
                });
            }
        }

        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        /// <param name="fileName">A project relative path to a source file that is a design time input</param>
        /// <returns>An XML description of the TempPE DLL for the specified file</returns>
        public Task<string> GetDesignTimeInputXmlAsync(string fileName)
        {
            if (_state == null)
            {
                throw new InvalidOperationException("Can't get design time input information until project information has been received");
            }

            // Make sure we're not being asked to compile a random file
            if (!_state.Inputs.Contains(fileName, StringComparers.Paths))
            {
                throw new ArgumentException("Can only get XML snippets for design time inputs", nameof(fileName));
            }

            // Remove the file from our todo list, in case it was in there.
            ImmutableInterlocked.TryRemove(ref _filesToCompile, fileName, out _);

            string outputFileName = GetOutputFileName(fileName);
            // make sure the file is up to date
            return _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(async accessor =>
            {
                bool compiled = await CompileDesignTimeInputAsync(accessor.Context, fileName, outputFileName, ignoreFileWriteTime: false);

                if (compiled)
                {
                    _telemetryService.PostEvent(TelemetryEventName.TempPECompileOnDemand);
                }

                return $@"<root>
  <Application private_binpath = ""{Path.GetDirectoryName(outputFileName)}""/>
  <Assembly
    codebase = ""{Path.GetFileName(outputFileName)}""
    name = ""{fileName}""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>";
            });
        }

        private async Task<bool> CompileDesignTimeInputAsync(IWorkspaceProjectContext context, string designTimeInput, string outputFileName, bool ignoreFileWriteTime, CancellationToken token = default)
        {
            HashSet<string> filesToCompile = GetFilesToCompile(designTimeInput);

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

        private string GetOutputFileName(string designTimeInput)
        {
            Assumes.NotNull(_state);

            // All monikers are project relative paths by definition (anything else is a link, and linked files can't be TempPE inputs), meaning 
            // the only invalid filename characters possible are path separators so we just replace them
            return Path.Combine(_state!.TempPEOutputPath, designTimeInput.Replace('\\', '.') + ".dll");
        }

        private bool CompilationNeeded(HashSet<string> files, string outputFileName)
        {
            if (!_fileSystem.FileExists(outputFileName))
            {
                return true;
            }

            try
            {
                DateTime outputDateTime = _fileSystem.LastFileWriteTimeUtc(outputFileName);

                foreach (string file in files)
                {
                    DateTime fileDateTime = _fileSystem.LastFileWriteTimeUtc(file);
                    if (fileDateTime > outputDateTime)
                        return true;
                }
            }
            // if we can't read the file time of the output file, then we presumably can't compile to it either, so returning false is appropriate.
            // if we can't read the file time of an input file, then we presumably can't read from it to compile either, so returning false is appropriate
            catch (IOException)
            { }
            catch (UnauthorizedAccessException)
            { }

            return false;
        }

        private HashSet<string> GetFilesToCompile(string moniker)
        {
            Assumes.NotNull(_state);

            // This is a HashSet because we allow files to be both inputs and shared inputs, and we don't want to compile the same file twice,
            // plus Roslyn needs to call Contains on this quite a lot in order to ensure its only compiling the right files so we want that to be fast.
            // When it comes to compiling the files there is no difference between shared and normal design time inputs, we just track differently because
            // shared are included in every DLL.
            var files = new HashSet<string>(_state!.SharedInputs.Count + 1, StringComparers.Paths);
            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs) so we can convert
            // them to full paths using MakeRooted.
            files.AddRange(_state.SharedInputs.Select(_project.MakeRooted));
            files.Add(_project.MakeRooted(moniker));
            return files;
        }
    }
}
