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
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.Threading.Tasks;

using InputTuple = System.Tuple<Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate, Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal partial class TempPEBuildManager : UnconfiguredProjectHostBridge<IProjectVersionedValue<InputTuple>, TempPEBuildManager.DesignTimeInputsDelta, IProjectVersionedValue<TempPEBuildManager.DesignTimeInputsItem>>, ITempPEBuildManager
    {
        protected readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IFileSystem _fileSystem;
        private readonly SequentialTaskExecutor _sequentialTaskQueue = new SequentialTaskExecutor();

        // protected for unit test purposes
        protected ITempPECompiler _compiler;
        protected Lazy<VSBuildManager> _buildManager;

        [ImportingConstructor]
        public TempPEBuildManager(IProjectThreadingService threadingService,
            IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ILanguageServiceHost languageServiceHost,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            VSLangProj.VSProjectEvents projectEvents,
            ITempPECompiler compiler,
            IFileSystem fileSystem)
             : base(threadingService.JoinableTaskContext)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _languageServiceHost = languageServiceHost;
            _projectSubscriptionService = projectSubscriptionService;
            _buildManager = new Lazy<VSBuildManager>(() => (VSBuildManager)projectEvents.BuildManagerEvents);
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
        public async Task NotifySourceFileDirtyAsync(string fileName)
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

            _buildManager.Value.OnDesignTimeOutputDirty(fileName);
        }

        public async Task<string> GetTempPEDescriptionXmlAsync(string fileName)
        {
            Requires.NotNull(fileName, nameof(fileName));

            await InitializeAsync();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            DesignTimeInputsItem inputs = AppliedValue.Value;

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
                        _fileSystem.RemoveFile(outputFileName);
                    }
                    catch (IOException)
                    { }
                    catch (UnauthorizedAccessException)
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
                    if (designTimeInputs.Add(item))
                    {
                        addedDesignTimeInputs.Add(item);
                    }
                }

                foreach (string item in value.RemovedItems)
                {
                    if (designTimeInputs.Remove(item))
                    {
                        removedDesignTimeInputs.Add(item);
                    }
                }

                foreach (string item in value.AddedSharedItems)
                {
                    if (designTimeSharedInputs.Add(item))
                    {
                        hasAddedDesignTimeSharedInputs = true;
                    }
                }

                foreach (string item in value.RemovedSharedItems)
                {
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
                newDesignTimeInputs = previousValue.Inputs;
                newSharedDesignTimeInputs = previousValue.SharedInputs;
            }

            // Apply our new value
            AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem
            {
                Inputs = newDesignTimeInputs,
                SharedInputs = newSharedDesignTimeInputs,
                // We always need an output path, so if it hasn't changed we just reuse the previous value
                OutputPath = value.OutputPath ?? previousValue.OutputPath
            }, value.DataSourceVersions);

            // Fire off any events necessary

            // Project properties changes cause all PEs to be dirty and possibly recompile
            if (value.HasProjectPropertyChanges)
            {
                foreach (string item in newDesignTimeInputs)
                {
                    await FireTempPEDirtyAsync(item, value.ShouldCompile);
                }
            }
            else
            {
                // Individual inputs dirty their PEs and possibly recompile
                foreach (string item in addedDesignTimeInputs)
                {
                    await FireTempPEDirtyAsync(item, value.ShouldCompile);
                }
            }
            // Shared items cause all TempPEs to be dirty, but don't recompile, to match legacy behaviour
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
                _buildManager.Value.OnDesignTimeOutputDeleted(item);
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
                _projectSubscriptionService.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(
                    linkOptions: new StandardRuleDataflowLinkOptions
                    {
                        RuleNames = Empty.OrdinalIgnoreCaseStringSet.Add(Compile.SchemaName),
                        PropagateCompletion = true,
                    }),
                _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(
                   linkOptions: new StandardRuleDataflowLinkOptions
                   {
                       RuleNames = Empty.OrdinalIgnoreCaseStringSet.Add(ConfigurationGeneral.SchemaName),
                       PropagateCompletion = true,
                   }),
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
            IProjectChangeDescription compileChanges = input.Value.Item1.ProjectChanges[Compile.SchemaName];
            IProjectChangeDescription configChanges = input.Value.Item2.ProjectChanges[ConfigurationGeneral.SchemaName];

            ImmutableArray<string>.Builder addedDesignTimeInputs = ImmutableArray.CreateBuilder<string>();
            ImmutableArray<string>.Builder removedDesignTimeInputs = ImmutableArray.CreateBuilder<string>();
            ImmutableArray<string>.Builder addedDesignTimeSharedInputs = ImmutableArray.CreateBuilder<string>();
            ImmutableArray<string>.Builder removedDesignTimeSharedInputs = ImmutableArray.CreateBuilder<string>();

            foreach (string item in compileChanges.Difference.AddedItems)
            {
                PreprocessAddItem(item);
            }

            foreach (string item in compileChanges.Difference.RemovedItems)
            {
                PreprocessRemoveItem(item);
            }

            foreach ((string oldName, string newName) in compileChanges.Difference.RenamedItems)
            {
                // A rename is just an add and a remove
                PreprocessAddItem(newName);
                PreprocessRemoveItem(oldName);
            }

            foreach (string item in compileChanges.Difference.ChangedItems)
            {
                (bool wasDesignTime, bool wasDesignTimeShared) = GetDesignTimePropsForItem(compileChanges.Before.Items[item]);
                (bool designTime, bool designTimeShared) = GetDesignTimePropsForItem(compileChanges.After.Items[item]);

                if (!wasDesignTime && designTime)
                {
                    addedDesignTimeInputs.Add(item);
                }
                else if (wasDesignTime && !designTime)
                {
                    removedDesignTimeInputs.Add(item);
                }
                if (!wasDesignTimeShared && designTimeShared)
                {
                    addedDesignTimeSharedInputs.Add(item);
                }
                else if (wasDesignTimeShared && !designTimeShared)
                {
                    removedDesignTimeSharedInputs.Add(item);
                }
            }

            bool namespaceChanged = configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.RootNamespaceProperty);

            string outputPath = null;
            if (configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.ProjectDirProperty) || configChanges.Difference.ChangedProperties.Contains(ConfigurationGeneral.IntermediateOutputPathProperty))
            {
                string basePath = configChanges.After.Properties[ConfigurationGeneral.ProjectDirProperty];
                string objPath = configChanges.After.Properties[ConfigurationGeneral.IntermediateOutputPathProperty];
                outputPath = Path.Combine(basePath, objPath, "TempPE");
            }

            var result = new DesignTimeInputsDelta
            {
                AddedItems = addedDesignTimeInputs.ToImmutable(),
                RemovedItems = removedDesignTimeInputs.ToImmutable(),
                AddedSharedItems = addedDesignTimeSharedInputs.ToImmutable(),
                RemovedSharedItems = removedDesignTimeSharedInputs.ToImmutable(),
                NamespaceChanged = namespaceChanged,
                OutputPath = outputPath,
                // if this is the first time processing (previousOutput = null) then we will be "adding" all inputs
                // but we don't want to immediately kick off a compile of all files. We'll let the events fire and
                // the file timestamp will determine if we compile when someone asks for the TempPEs
                ShouldCompile = (previousOutput != null),

                DataSourceVersions = input.DataSourceVersions
            };

            return Task.FromResult(result);

            (bool designTime, bool designTimeShared) GetDesignTimePropsForItem(IImmutableDictionary<string, string> item)
            {
                item.TryGetValue(Compile.LinkProperty, out string linkString);
                item.TryGetValue(Compile.DesignTimeProperty, out string designTimeString);
                item.TryGetValue(Compile.DesignTimeSharedInputProperty, out string designTimeSharedString);

                if (linkString != null && linkString.Length > 0)
                {
                    // Linked files are never used as TempPE inputs
                    return (false, false);
                }

                return (StringComparers.PropertyValues.Equals(designTimeString, bool.TrueString), StringComparers.PropertyValues.Equals(designTimeSharedString, bool.TrueString));
            }

            void PreprocessAddItem(string item)
            {
                (bool designTime, bool designTimeShared) = GetDesignTimePropsForItem(compileChanges.After.Items[item]);

                if (designTime)
                {
                    addedDesignTimeInputs.Add(item);
                }

                // Legacy allows files to be DesignTime and DesignTimeShared
                if (designTimeShared)
                {
                    addedDesignTimeSharedInputs.Add(item);
                }
            }

            void PreprocessRemoveItem(string item)
            {
                // Because the item has been removed we retreive its properties from the Before state
                (bool designTime, bool designTimeShared) = GetDesignTimePropsForItem(compileChanges.Before.Items[item]);

                if (designTime)
                {
                    removedDesignTimeInputs.Add(item);
                }

                // Legacy allows files to be DesignTime and DesignTimeShared
                if (designTimeShared)
                {
                    removedDesignTimeSharedInputs.Add(item);
                }
            }
        }

        protected override bool ShouldValueBeApplied(DesignTimeInputsDelta previouslyAppliedOutput, DesignTimeInputsDelta newOutput)
        {
            return newOutput.HasFileChanges || newOutput.HasProjectPropertyChanges;
        }
    }
}
