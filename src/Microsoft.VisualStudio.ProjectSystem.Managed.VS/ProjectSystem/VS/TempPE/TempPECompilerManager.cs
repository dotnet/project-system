// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class TempPECompilerManager : OnceInitializedOnceDisposedAsync
    {
        private static readonly TimeSpan s_compilationDelayTime = TimeSpan.FromMilliseconds(500);

        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IDesignTimeInputsDataSource _inputsDataSource;
        private readonly IDesignTimeInputsFileWatcher _fileWatcher;
        private readonly ITempPECompiler _compiler;
        private readonly IFileSystem _fileSystem;
        private readonly TaskDelayScheduler _scheduler;

        private readonly DisposableBag _disposables = new DisposableBag();

        private ImmutableArray<string> _designTimeInputs;
        private ImmutableArray<string> _sharedDesignTimeInputs;
        private ImmutableDictionary<string, bool> _filesToCompile = ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparers.Paths); // Key is filename, value is whether to ignore the last write time check
        private string? _outputPath;
        private readonly TaskCompletionSource<bool> _receivedProjectRuleSource = new TaskCompletionSource<bool>();

        private ITargetBlock<IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>? _inputsActionBlock;
        private ITargetBlock<IProjectVersionedValue<string[]>>? _fileWatcherActionBlock;

        [ImportingConstructor]
        public TempPECompilerManager(ConfiguredProject project,
                                     IProjectSubscriptionService projectSubscriptionService,
                                     IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost,
                                     IProjectThreadingService threadingService,
                                     IDesignTimeInputsDataSource inputsDataSource,
                                     IDesignTimeInputsFileWatcher fileWatcher,
                                     ITempPECompiler compiler,
                                     IFileSystem fileSystem)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
            _inputsDataSource = inputsDataSource;
            _fileWatcher = fileWatcher;
            _compiler = compiler;
            _fileSystem = fileSystem;

            _scheduler = new TaskDelayScheduler(s_compilationDelayTime, threadingService, CancellationToken.None);
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Create an action block process the design time inputs and configuration general changes
            _inputsActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>>>(ProcessDataflowChanges);

            IDisposable projectLink = ProjectDataSources.SyncLinkTo(
                   _inputsDataSource.SourceBlock.SyncLinkOptions(
                       linkOptions: new StandardRuleDataflowLinkOptions
                       {
                           PropagateCompletion = true,
                       }),
                   _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(
                      linkOptions: new StandardRuleDataflowLinkOptions
                      {
                          RuleNames = Empty.OrdinalIgnoreCaseStringSet.Add(ConfigurationGeneral.SchemaName),
                          PropagateCompletion = true,
                      }),
                   _inputsActionBlock,
                   DataflowOption.PropagateCompletion,
                   cancellationToken: _project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);

            // Create an action block to process file change notifications
            _fileWatcherActionBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<string[]>>(ProcessFileChangeNotification);
            IDisposable watcherLink = _fileWatcher.SourceBlock.LinkTo(_fileWatcherActionBlock, DataflowOption.PropagateCompletion);

            _disposables.AddDisposable(projectLink);
            _disposables.AddDisposable(watcherLink);

            return Task.CompletedTask;
        }

        internal void ProcessFileChangeNotification(IProjectVersionedValue<string[]> arg)
        {
            // Ignore any updates until we've received the first set of design time inputs (which shouldn't happen anyway)
            // That first update will queue all of the files so we're not losing anything
            if (_designTimeInputs == null)
            {
                return;
            }

            foreach (string changedFile in arg.Value)
            {
                // if a shared input changes, we recompile everything
                if (_sharedDesignTimeInputs.Contains(changedFile))
                {
                    foreach (string file in _designTimeInputs)
                    {
                        ImmutableInterlocked.TryAdd(ref _filesToCompile, file, /* ignoreWriteTime */ false);
                    }
                    // Since we've just queued every file, we don't care about any other changes
                    break;
                }
                else
                {
                    // A normal design time input, so just add it to the queue
                    ImmutableInterlocked.TryAdd(ref _filesToCompile, changedFile, /* ignoreWriteTime */ false);
                }
            }

            QueueCompilation();
        }

        internal void ProcessDataflowChanges(IProjectVersionedValue<Tuple<DesignTimeInputs, IProjectSubscriptionUpdate>> input)
        {
            DesignTimeInputs inputs = input.Value.Item1;

            IProjectChangeDescription configChanges = input.Value.Item2.ProjectChanges[ConfigurationGeneral.SchemaName];

            // On the first call where we receive design time inputs we queue compilation of all of them, knowing that we'll only compile if the file write date requires it
            if (_designTimeInputs == null)
            {
                AddAllInputsToQueue(false);
            }
            else
            {
                // If its not the first call...

                // If a new shared design time input is added, we need to recompile everything regardless of source file modified date
                // because it could be an old file that is being promoted to a shared input
                if (inputs.SharedInputs.Except(_sharedDesignTimeInputs, StringComparers.Paths).Any())
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
                    foreach (string file in inputs.Inputs.Except(_designTimeInputs, StringComparers.Paths))
                    {
                        ImmutableInterlocked.TryAdd(ref _filesToCompile, file, /* ignoreWriteTime */ false);
                    }
                }
            }

            // Make sure we have the up to date list of inputs
            _designTimeInputs = inputs.Inputs;
            _sharedDesignTimeInputs = inputs.SharedInputs;

            // Make sure we have the up to date output path
            string basePath = configChanges.After.Properties[ConfigurationGeneral.ProjectDirProperty];
            string objPath = configChanges.After.Properties[ConfigurationGeneral.IntermediateOutputPathProperty];
            _outputPath = GetOutputPath(basePath, objPath);

            // Remove any files in the queue that we don't care about any more
            foreach (string file in _filesToCompile.Keys)
            {
                if (!inputs.Inputs.Contains(file))
                {
                    ImmutableInterlocked.TryRemove(ref _filesToCompile, file, out _);
                }
            }

            _receivedProjectRuleSource.TrySetResult(true);

            QueueCompilation();

            void AddAllInputsToQueue(bool skipFileWriteChecks)
            {
                foreach (string file in inputs.Inputs)
                {
                    ImmutableInterlocked.TryAdd(ref _filesToCompile, file, skipFileWriteChecks);
                }
            }
        }

        internal static string GetOutputPath(string projectPath, string intermediateOutputPath)
        {
            return Path.Combine(projectPath, intermediateOutputPath, "TempPE");
        }

        private void QueueCompilation()
        {
            if (_filesToCompile.Count > 0)
            {
                _scheduler.ScheduleAsyncTask(ProcessCompileQueue, _project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            // This will stop our blocks taking any more input
            _inputsActionBlock?.Complete();
            _fileWatcherActionBlock?.Complete();

            if (_inputsActionBlock != null)
            {
                await Task.WhenAll(_inputsActionBlock.Completion, _fileWatcherActionBlock!.Completion);
            }

            // By waiting for completion we know that the following dispose will cancel any pending compilations, and there won't be any more
            _scheduler.Dispose();
            _disposables.Dispose();
        }

        private async Task ProcessCompileQueue(CancellationToken token)
        {
            await _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(async accessor =>
            {
                // Grab the next file to compile off the queue
                (string fileName, bool ignoreFileWriteTime) = _filesToCompile.FirstOrDefault();
                while (fileName != null)
                {
                    if (IsDisposing || IsDisposed)
                    {
                        return;
                    }

                    token.ThrowIfCancellationRequested();

                    // Remove the file from our todo list. If it wasn't there (because it was removed as a design time input while we were busy) we don't need to compile it
                    bool wasInQueue = ThreadingTools.ApplyChangeOptimistically(ref _filesToCompile, fileName, (s, f) => s.Remove(f));

                    string outputFileName = await GetOutputFileName(fileName);
                    if (wasInQueue)
                    {
                        await CompileDesignTimeInput(accessor.Context, fileName, outputFileName, ignoreFileWriteTime, token);
                    }

                    // Grab another file off the queue
                    (fileName, ignoreFileWriteTime) = _filesToCompile.FirstOrDefault();
                }
            });
        }

        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string> GetDesignTimeInputXML(string fileName)
        {
            // Make sure we're not being asked to compile a random file
            if (!_designTimeInputs.Contains(fileName, StringComparers.Paths))
            {
                throw new ArgumentException("Can only get XML snippets for design time inputs", nameof(fileName));
            }

            // Remove the file from our todo list, in case it was in there.
            ThreadingTools.ApplyChangeOptimistically(ref _filesToCompile, fileName, (s, f) => s.Remove(f));

            string outputFileName = await GetOutputFileName(fileName);
            // make sure the file is up to date
            await _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(async accessor =>
            {
                await CompileDesignTimeInput(accessor.Context, fileName, outputFileName, ignoreFileWriteTime: false);
            });

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
        }

        private async Task CompileDesignTimeInput(IWorkspaceProjectContext context, string designTimeInput, string outputFileName, bool ignoreFileWriteTime, CancellationToken token = default)
        {
            HashSet<string> filesToCompile = GetFilesToCompile(designTimeInput);

            if (ignoreFileWriteTime || CompilationNeeded(filesToCompile, outputFileName))
            {
                bool result = await _compiler.CompileAsync(context, outputFileName, filesToCompile, token);

                // if the compilation failed or was cancelled we should clean up any old TempPE outputs lest a designer gets the wrong types, plus its what legacy did
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
        }

        private async Task<string> GetOutputFileName(string designTimeInput)
        {
            // Wait until we've received at least one project rule update, or we won't know where to put the file
            await _receivedProjectRuleSource.Task;

            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs), meaning 
            // the only invalid filename characters possible are path separators so we just replace them
            return Path.Combine(_outputPath, designTimeInput.Replace('\\', '.') + ".dll");
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
            // This is a HashSet because we allow files to be both inputs and shared inputs, and we don't want to compile the same file twice,
            // plus Roslyn needs to call Contains on this quite a lot in order to ensure its only compiling the right files so we want that to be fast.
            // When it comes to compiling the files there is no difference between shared and normal design time inputs, we just track differently because
            // shared are included in every DLL.
            var files = new HashSet<string>(_sharedDesignTimeInputs.Length + 1, StringComparers.Paths);
            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs) so we can convert
            // them to full paths using MakeRooted.
            files.AddRange(_sharedDesignTimeInputs.Select(_project.UnconfiguredProject.MakeRooted));
            files.Add(_project.UnconfiguredProject.MakeRooted(moniker));
            return files;
        }
    }
}
