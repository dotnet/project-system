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

using Microsoft.Build.Execution;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.Threading.Tasks;

using InputTuple = System.Tuple<Microsoft.VisualStudio.ProjectSystem.IProjectSnapshot, Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : UnconfiguredProjectHostBridge<IProjectVersionedValue<InputTuple>, DesignTimeInputsDelta, IProjectVersionedValue<DesignTimeInputsItem>>, ITempPEBuildManager
    {
        protected readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IFileSystem _fileSystem;
        private readonly SequentialTaskExecutor _sequentialTaskQueue = new SequentialTaskExecutor();

        // protected for unit test purposes
        protected ITempPECompiler _compiler;
        protected VSBuildManager _buildManager;

        [ImportingConstructor]
        public TempPEBuildManager(IProjectThreadingService threadingService,
            IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ILanguageServiceHost languageServiceHost,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            [Import(typeof(VSLangProj.BuildManager))]VSBuildManager buildManager,
            ITempPECompiler compiler,
            IFileSystem fileSystem)
             : base(threadingService.JoinableTaskContext)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _languageServiceHost = languageServiceHost;
            _projectSubscriptionService = projectSubscriptionService;
            _buildManager = buildManager;
            _compiler = compiler;
            _fileSystem = fileSystem;
        }

        public string[] GetTempPESourceFileNames()
        {
            Initialize();

            return AppliedValue.Value.Inputs.ToArray();
        }

        /// <summary>
        /// Called externally to fire the TempPEDirty events if the provided fileName is one of the known TempPE inputs or shared inputs
        /// </summary>
        public async Task TryFireTempPEDirtyAsync(string fileName)
        {
            // This method gets called for any file change but unless someone has asked for TempPE information this
            // class will be uninitialized, so just exit early
            if (!IsInitialized) return;

            DesignTimeInputsItem inputs = AppliedValue.Value;

            // This might be overkill, but none of the default .NET Core templates have any shared design time files
            // Non shared design time files will be more common (just needs one .resx files), but its still reasonable to assume these collections will be empty a lot of the time
            // Since this method is called every time a file is saved checking Count saves us 54ns for the fast path, and only costs 1ns the rest of the time
            if (inputs.SharedInputs.Count > 0 && inputs.SharedInputs.Contains(fileName))
            {
                foreach (string item in inputs.Inputs)
                {
                    await FireTempPEDirtyAsync(item, false);
                }
            }
            else if (inputs.Inputs.Count > 0 && inputs.Inputs.Contains(fileName))
            {
                await FireTempPEDirtyAsync(fileName, true);
            }
        }

        /// <summary>
        /// Called privately to actually fire the TempPE events and optionally recompile the TempPE library for the specified inpu
        /// </summary>
        private async Task FireTempPEDirtyAsync(string fileName, bool shouldCompile)
        {
            // Not using use the ThreadingService property because unit tests
            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            if (shouldCompile)
            {
                DesignTimeInputsItem inputs = AppliedValue.Value;
                HashSet<string> files = GetFilesToCompile(fileName, inputs.SharedInputs);
                string outputFileName = GetOutputFileName(inputs.OutputPath, fileName);
                await CompileTempPEAsync(files, outputFileName);
            }

            _buildManager.OnDesignTimeOutputDirty(fileName);
        }

        public async Task<string> GetTempPEDescriptionXmlAsync(string fileName)
        {
            await InitializeAsync();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            DesignTimeInputsItem inputs = AppliedValue.Value;
            if (fileName == null)
                throw new ArgumentException("Must supply a file to build", nameof(fileName));

            if (!inputs.Inputs.Contains(fileName))
                throw new ArgumentException("FileName supplied must be one of the DesignTime source files", nameof(fileName));

            string outputPath = inputs.OutputPath;

            HashSet<string> files = GetFilesToCompile(fileName, inputs.SharedInputs);
            string outputFileName = GetOutputFileName(outputPath, fileName);
            if (CompilationNeeded(files, outputFileName))
            {
                // For parity with legacy we don't care about the compilation result: Legacy only errors here if it runs out of memory queuing the compilation
                await CompileTempPEAsync(files, outputFileName);
            }

            // VSTypeResolutionService is the only consumer, and it only uses the codebase element so it's fine to default most of these (VC++ does the same)
            return $@"<root>
  <Application private_binpath = ""{outputPath}""/>
  <Assembly
    codebase = ""{outputFileName}""
    name = ""{fileName}""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>";
        }

        internal bool CompilationNeeded(HashSet<string> files, string outputFileName)
        {
            if (!_fileSystem.FileExists(outputFileName))
                return true;

            DateTime outputDateTime = _fileSystem.LastFileWriteTime(outputFileName);

            foreach (string file in files)
            {
                DateTime fileDateTime = _fileSystem.LastFileWriteTime(file);
                if (fileDateTime > outputDateTime)
                    return true;
            }

            return false;
        }

        private static string GetOutputFileName(string outputPath, string fileName)
        {
            return Path.Combine(outputPath, fileName.Replace('\\', '.') + ".dll");
        }

        protected virtual Task CompileTempPEAsync(HashSet<string> filesToCompile, string outputFileName)
        {
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                bool result = await _compiler.CompileAsync(_languageServiceHost.ActiveProjectContext, outputFileName, filesToCompile, default);

                // if the compilation failed or was cancelled we should clean up any old TempPE outputs lest a designer gets the wrong types, plus its what legacy did
                if (!result)
                {
                    try
                    {
                        if (_fileSystem.FileExists(outputFileName))
                        {
                            _fileSystem.RemoveFile(outputFileName);
                        }
                    }
                    catch (IOException)
                    { }
                }
            });
        }

        protected virtual HashSet<string> GetFilesToCompile(string fileName, ImmutableHashSet<string> sharedInputs)
        {
            // This is a HashSet because we allow files to be both inputs and shared inputs, and we don't want to compile the same file twice
            // plus Roslyn needs to call Contains on this quite a lot in order to ensure its only compiling the right files so we want that to be fast.
            var files = new HashSet<string>(sharedInputs.Count + 1, StringComparers.Paths);
            files.AddRange(sharedInputs.Select(UnconfiguredProject.MakeRooted));
            files.Add(UnconfiguredProject.MakeRooted(fileName));
            return files;
        }

        /// <summary>
        /// ApplyAsync is called on the UI thread and its job is to update AppliedValue to be correct based on the changes that have come through data flow after being processed
        /// </summary>
        protected override async Task ApplyAsync(DesignTimeInputsDelta value)
        {
            // Not using use the ThreadingService property because unit tests
            _unconfiguredProjectServices.ThreadingService.VerifyOnUIThread();

            DesignTimeInputsItem previousValue = AppliedValue.Value;

            // Calculate the new value
            ImmutableHashSet<string> newDesignTimeInputs;
            ImmutableHashSet<string> newSharedDesignTimeInputs;
            var addedDesignTimeInputs = new List<string>();
            var removedDesignTimeInputs = new List<string>();
            bool hasRemovedDesignTimeSharedInputs = false;
            bool hasAddedDesignTimeSharedInputs = false;

            if (value.HasFileChanges)
            {
                ImmutableHashSet<string>.Builder designTimeInputs = previousValue.Inputs.ToBuilder();
                ImmutableHashSet<string>.Builder designTimeSharedInputs = previousValue.SharedInputs.ToBuilder();

                foreach (string item in value.AddedItems)
                {
                    // If a property changes we might be asked to add items that already exist, so we just ignore them as we're happy using our existing CancellationSeries
                    if (designTimeInputs.Add(item))
                    {
                        addedDesignTimeInputs.Add(item);
                    }
                }

                foreach (string item in value.AddedSharedItems)
                {
                    if (designTimeSharedInputs.Add(item))
                    {
                        hasAddedDesignTimeSharedInputs = true;
                    }
                }

                foreach (string item in value.RemovedItems)
                {
                    if (designTimeInputs.Remove(item))
                    {
                        removedDesignTimeInputs.Add(item);
                    }
                    if (designTimeSharedInputs.Remove(item))
                    {
                        hasRemovedDesignTimeSharedInputs = true;
                    }
                }

                newDesignTimeInputs = designTimeInputs.ToImmutable();
                newSharedDesignTimeInputs = designTimeSharedInputs.ToImmutable();
            }
            else
            {
                // If there haven't been file changes we can just flow our previous collections to the new version to avoid roundtriping our collections to builders and back
                // Normally ShouldValueBeApplied can be used for this but our project properties require comparison to previous values so we have to apply every update
                newDesignTimeInputs = previousValue.Inputs;
                newSharedDesignTimeInputs = previousValue.SharedInputs;
            }

            // Apply our new value
            AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem
            {
                Inputs = newDesignTimeInputs,
                SharedInputs = newSharedDesignTimeInputs,
                RootNamespace = value.RootNamespace,
                OutputPath = value.OutputPath
            }, value.DataSourceVersions);


            // Fire off any events necessary

            // We ignore blank values for previous value so that the initial load of the project doesn't trigger a compile of all PEs. It also makes testing easier.
            bool hasHadProjectChange = (!string.IsNullOrEmpty(previousValue.RootNamespace) && !string.Equals(previousValue.RootNamespace, value.RootNamespace, StringComparison.Ordinal)) ||
                                       (!string.IsNullOrEmpty(previousValue.OutputPath) && !string.Equals(previousValue.OutputPath, value.OutputPath, StringComparisons.Paths));

            // Project properties and shared items cause all TempPEs to be dirty, because shared items are included in all TempPEs
            if (hasHadProjectChange)
            {
                foreach (string item in newDesignTimeInputs)
                {
                    await FireTempPEDirtyAsync(item, value.ShouldCompile);
                }
            }
            else
            {
                // otherwise just the ones that we've added are dirty
                foreach (string item in addedDesignTimeInputs)
                {
                    await FireTempPEDirtyAsync(item, value.ShouldCompile);
                }
            }
            // We don't recompile for shared design time inputs
            if (hasRemovedDesignTimeSharedInputs || hasAddedDesignTimeSharedInputs)
            {
                // adding or removing shared design time inputs dirties things but doesn't recompile
                foreach (string item in newDesignTimeInputs)
                {
                    // We don't want to fire again if we already fired above and compiled
                    if (!addedDesignTimeInputs.Contains(item))
                    {
                        await FireTempPEDirtyAsync(item, false);
                    }
                }
            }
            foreach (string item in removedDesignTimeInputs)
            {
                _buildManager.OnDesignTimeOutputDeleted(item);
            }
        }

        /// <summary>
        /// InitializeInnerCoreAsync is responsible for setting an initial AppliedValue. This value will be used by any UI thread calls that may happen
        /// before the first data flow blocks have been processed. If this method doesn't return a value then the system will block until the first blocks
        /// have been applied but since we are initialized on the UI thread, that blocks for us, so we must return an initial state.
        /// </summary>
        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken)
        {
            AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem(), ImmutableDictionary.Create<NamedIdentity, IComparable>());

            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is where we tell data flow which blocks we're interested in receiving updates for
        /// </summary>
        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<InputTuple>> targetBlock)
        {
            return ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                _projectSubscriptionService.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                cancellationToken: ProjectAsynchronousTasksService.UnloadCancellationToken);
        }

        /// <summary>
        /// Preprocess gets called as each data flow block updates and its job is to take the input from those blocks and do whatever work needed
        /// so that ApplyAsync has all of the info it needs to do its job.
        /// </summary>
        protected override Task<DesignTimeInputsDelta> PreprocessAsync(IProjectVersionedValue<InputTuple> input, DesignTimeInputsDelta previousOutput)
        {
            ProjectInstance project = input.Value.Item1.ProjectInstance;
            IProjectChangeDescription changes = input.Value.Item2.ProjectChanges[Compile.SchemaName];

            ImmutableArray<string>.Builder addedDesignTimeInputs = ImmutableArray.CreateBuilder<string>();
            ImmutableArray<string>.Builder removedDesignTimeInputs = ImmutableArray.CreateBuilder<string>();
            ImmutableArray<string>.Builder addedDesignTimeSharedInputs = ImmutableArray.CreateBuilder<string>();

            foreach (string item in changes.Difference.AddedItems)
            {
                PreprocessAddItem(item);
            }

            foreach (string item in changes.Difference.RemovedItems)
            {
                // the RemovedItems doesn't have any project info any more so we can't tell if an item was a design time input or not
                // and we can't check in AppliedValue because there could be an unapplied value that adds the item that is still in the queue
                // so we have to just process the removed items in ApplyAsync and take advantage of the fact that its
                // quick to remove from immutable collections, even if the item doesn't exist, because they're implemented as trees
                removedDesignTimeInputs.Add(item);
            }

            foreach ((string oldName, string newName) in changes.Difference.RenamedItems)
            {
                // A rename is just an add and a remove
                PreprocessAddItem(newName);
                removedDesignTimeInputs.Add(oldName);
            }

            foreach (string item in changes.Difference.ChangedItems)
            {
                // Items appear in this set if an items metadata changes. 
                // For example that could be when DesignTime goes from false to true, so we care about it, but we don't know which metadata changed or what the old value was
                // so there is a high chance this is an irrelevant change to us.

                // The actual logic here is deceptively simple, so what is actually happening is this:
                // Previous Design Time   | New Design Time   | Action
                // ----------------------------------------------------------------
                // False                  | False             | PreprocessAddItem will return false, ApplyAsync will process remove, which is fast enough to not worry about
                // False                  | True              | PreprocessAddItem will return true, ApplyAsync will process as an add, so all good
                // True                   | False             | PreprocessAddItem will return false, ApplyAsync will process remove, so all good
                // True                   | True              | PreprocessAddItem will return true, ApplyAsync will not process add because key already exists
                if (!PreprocessAddItem(item))
                {
                    removedDesignTimeInputs.Add(item);
                }
            }

            string rootNamespace = project.GetPropertyValue(ConfigurationGeneral.RootNamespaceProperty);

            string basePath = project.GetPropertyValue(ConfigurationGeneral.ProjectDirProperty);
            string objPath = project.GetPropertyValue(ConfigurationGeneral.IntermediateOutputPathProperty);
            string outputPath = Path.Combine(basePath, objPath, "TempPE");

            var result = new DesignTimeInputsDelta
            {
                AddedItems = addedDesignTimeInputs.ToImmutable(),
                RemovedItems = removedDesignTimeInputs.ToImmutable(),
                AddedSharedItems = addedDesignTimeSharedInputs.ToImmutable(),
                RootNamespace = rootNamespace,
                OutputPath = outputPath,
                // if this is the first time processing (previousOutput = null) then we will be "adding" all inputs
                // but we don't want to immediately kick off a compile of all files. We'll let the events fire and
                // the file timestamp will determine if we compile when someone asks for the TempPEs
                ShouldCompile = (previousOutput != null),

                DataSourceVersions = input.DataSourceVersions
            };

            return Task.FromResult(result);

            bool PreprocessAddItem(string item)
            {
                ProjectItemInstance projItem = project.GetItemsByItemTypeAndEvaluatedInclude(Compile.SchemaName, item).FirstOrDefault();
                System.Diagnostics.Debug.Assert(projItem != null, "Couldn't find the project item for Compile with an evaluated include of " + item);

                bool added = false;
                bool link = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.LinkProperty), bool.TrueString);
                if (!link)
                {
                    bool designTime = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.DesignTimeProperty), bool.TrueString);
                    bool designTimeShared = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.DesignTimeSharedInputProperty), bool.TrueString);

                    if (designTime)
                    {
                        addedDesignTimeInputs.Add(item);
                        added = true;
                    }

                    // Legacy allows files to be DesignTime and DesignTimeShared
                    if (designTimeShared)
                    {
                        addedDesignTimeSharedInputs.Add(item);
                        added = true;
                    }
                }
                return added;
            }
        }
    }

    internal class DesignTimeInputsItem
    {
        public ImmutableHashSet<string> Inputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
        public ImmutableHashSet<string> SharedInputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
        public string OutputPath { get; internal set; }
        public string RootNamespace { get; internal set; }
    }

    internal class DesignTimeInputsDelta
    {
        public bool ShouldCompile { get; set; } = true;
        public ImmutableArray<string> AddedSharedItems { get; set; } = ImmutableArray.Create<string>();
        public ImmutableArray<string> AddedItems { get; set; } = ImmutableArray.Create<string>();
        public ImmutableArray<string> RemovedItems { get; set; } = ImmutableArray.Create<string>();
        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; set; } = ImmutableSortedDictionary.Create<NamedIdentity, IComparable>();
        public string RootNamespace { get; internal set; }
        public string OutputPath { get; internal set; }
        public bool HasFileChanges => AddedSharedItems.Length > 0 || AddedItems.Length > 0 || RemovedItems.Length > 0;
    }
}
