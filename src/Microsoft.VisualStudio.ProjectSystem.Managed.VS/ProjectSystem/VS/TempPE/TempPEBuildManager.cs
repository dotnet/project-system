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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

using InputTuple = System.Tuple<Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate, Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageServiceWindowsForms)]
    internal partial class TempPEBuildManager : UnconfiguredProjectHostBridge<IProjectVersionedValue<InputTuple>, TempPEBuildManager.DesignTimeInputsDelta, IProjectVersionedValue<TempPEBuildManager.DesignTimeInputsItem>>, ITempPEBuildManager, IProjectDynamicLoadComponent
    {
        private static readonly TimeSpan s_compilationWaitTime = TimeSpan.FromMilliseconds(500);

        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IFileSystem _fileSystem;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly ITempPECompiler _compiler;

        // This field is protected to enable easier unit testing
        protected VSBuildManager BuildManager;

        [ImportingConstructor]
        public TempPEBuildManager(IProjectThreadingService threadingService,
                                  IUnconfiguredProjectCommonServices unconfiguredProjectServices,
                                  IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost,
                                  IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
                                  ITempPECompiler compiler,
                                  IFileSystem fileSystem,
                                  IProjectFaultHandlerService projectFaultHandlerService,
                                  VSBuildManager buildManager)
             : base(threadingService.JoinableTaskContext)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
            _projectSubscriptionService = projectSubscriptionService;
            _compiler = compiler;
            _fileSystem = fileSystem;
            _projectFaultHandlerService = projectFaultHandlerService;
            BuildManager = buildManager;
        }

        public string[] GetTempPEMonikers()
        {
            Initialize();

            return AppliedValue.Value.Inputs.ToArray();
        }

        /// <summary>
        /// Called privately to actually fire the TempPE events and optionally recompile the TempPE library for the specified input
        /// </summary>
        private async Task FireTempPEDirtyAsync(string moniker, bool shouldCompile)
        {
            // Not using use the ThreadingService property because unit tests
            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            if (shouldCompile)
            {
                await ScheduleCompilationAsync(moniker, AppliedValue.Value, waitForCompletion: false, forceCompilation: true);
            }
            BuildManager.OnDesignTimeOutputDirty(moniker);
        }

        public async Task<string> GetTempPEDescriptionXmlAsync(string moniker)
        {
            Requires.NotNull(moniker, nameof(moniker));

            await InitializeAsync();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            DesignTimeInputsItem inputs = AppliedValue.Value;

            if (!inputs.Inputs.Contains(moniker))
                throw new ArgumentException("Moniker supplied must be one of the DesignTime source files", nameof(moniker));

            string outputPath = inputs.OutputPath;
            string outputFileName = GetOutputFileName(outputPath, moniker);

            // For parity with legacy we don't care about the compilation result: Legacy only errors here if it runs out of memory queuing the compilation
            // Additionally for parity, we compile here on the UI thread and block (whilst still preventing simultaneous work)
            await ScheduleCompilationAsync(moniker, inputs, waitForCompletion: true, forceCompilation: false);

            // VSTypeResolutionService is the only consumer, and it only uses the codebase element so it's fine to default most of these (VC++ does the same)
            return $@"<root>
  <Application private_binpath = ""{outputPath}""/>
  <Assembly
    codebase = ""{outputFileName}""
    name = ""{moniker}""
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

        private static string GetOutputFileName(string outputPath, string moniker)
        {
            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs), meaning 
            // the only invalid filename characters possible are path separators so we just replace them
            return Path.Combine(outputPath, moniker.Replace('\\', '.') + ".dll");
        }

        private async Task ScheduleCompilationAsync(string moniker, DesignTimeInputsItem inputs, bool waitForCompletion, bool forceCompilation)
        {
            HashSet<string> files = GetFilesToCompile(moniker, inputs.SharedInputs);
            string outputFileName = GetOutputFileName(inputs.OutputPath, moniker);

            if (forceCompilation || CompilationNeeded(files, outputFileName))
            {
                if (inputs.TaskSchedulers.TryGetValue(moniker, out ITaskDelayScheduler scheduler))
                {
                    if (waitForCompletion)
                    {
                        await scheduler.RunAsyncTask(token => CompileTempPEAsync(files, outputFileName, token)).Task;
                    }
                    else
                    {
                        _projectFaultHandlerService.Forget(scheduler.ScheduleAsyncTask(token => CompileTempPEAsync(files, outputFileName, token)).Task, UnconfiguredProject);
                    }
                }
            }
        }

        protected virtual async Task CompileTempPEAsync(HashSet<string> filesToCompile, string outputFileName, CancellationToken token)
        {
            bool result = await _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(accessor =>
            {
                return _compiler.CompileAsync(accessor.Context, outputFileName, filesToCompile, token);
            });

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

        protected virtual HashSet<string> GetFilesToCompile(string moniker, ImmutableHashSet<string> sharedInputs)
        {
            // This is a HashSet because we allow files to be both inputs and shared inputs, and we don't want to compile the same file twice,
            // plus Roslyn needs to call Contains on this quite a lot in order to ensure its only compiling the right files so we want that to be fast.
            // When it comes to compiling the files there is no difference between shared and normal design time inputs, we just track differently because
            // shared are included in every DLL.
            var files = new HashSet<string>(sharedInputs.Count + 1, StringComparers.Paths);
            // All monikers are project relative paths by defintion (anything else is a link, and linked files can't be TempPE inputs) so we can convert
            // them to full paths using MakeRooted.
            files.AddRange(sharedInputs.Select(UnconfiguredProject.MakeRooted));
            files.Add(UnconfiguredProject.MakeRooted(moniker));
            return files;
        }

        /// <summary>
        /// ApplyAsync is called on the UI thread and its job is to update AppliedValue to be correct based on the changes that have come through data flow after being processed
        /// </summary>
        protected override async Task ApplyAsync(DesignTimeInputsDelta value)
        {
            // Not using use the ThreadingService property because unit tests
            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            DesignTimeInputsItem previousValue = AppliedValue?.Value ?? new DesignTimeInputsItem();

            // Calculate the new value
            ImmutableHashSet<string> newDesignTimeInputs;
            ImmutableHashSet<string> newSharedDesignTimeInputs;
            ImmutableDictionary<string, uint> newCookies;
            ImmutableDictionary<string, ITaskDelayScheduler> newSchedulers;
            var addedDesignTimeInputs = new List<string>();
            var removedDesignTimeInputs = new List<string>();
            bool hasRemovedDesignTimeSharedInputs = false;
            bool hasAddedDesignTimeSharedInputs = false;

            if (value.HasFileChanges)
            {
                var designTimeInputs = previousValue.Inputs.ToBuilder();
                var designTimeSharedInputs = previousValue.SharedInputs.ToBuilder();
                var cookies = previousValue.Cookies.ToBuilder();
                var schedulers = previousValue.TaskSchedulers.ToBuilder();

                foreach (string item in value.AddedItems)
                {
                    if (designTimeInputs.Add(item))
                    {
                        addedDesignTimeInputs.Add(item);
                        schedulers.Add(item, CreateTaskScheduler());
                    }
                }

                foreach (string item in value.RemovedItems)
                {
                    if (designTimeInputs.Remove(item))
                    {
                        removedDesignTimeInputs.Add(item);
                        // We only unsubscribe from file changes if there is no other reason to care about this file
                        if (TryGetValueIfUnused(item, cookies, designTimeSharedInputs, out _))
                        {
                            cookies.Remove(item);
                        }
                        if (TryGetValueIfUnused(item, schedulers, designTimeSharedInputs, out ITaskDelayScheduler scheduler))
                        {
                            schedulers.Remove(item);
                            scheduler.Dispose();
                        }
                    }
                }

                foreach (string item in value.AddedSharedItems)
                {
                    if (designTimeSharedInputs.Add(item))
                    {
                        hasAddedDesignTimeSharedInputs = true;
                        if (!schedulers.ContainsKey(item))
                        {
                            schedulers.Add(item, CreateTaskScheduler());
                        }
                    }
                }

                foreach (string item in value.RemovedSharedItems)
                {
                    if (designTimeSharedInputs.Remove(item))
                    {
                        hasRemovedDesignTimeSharedInputs = true;
                        if (TryGetValueIfUnused(item, cookies, designTimeInputs, out _))
                        {
                            cookies.Remove(item);
                        }
                        if (TryGetValueIfUnused(item, schedulers, designTimeInputs, out ITaskDelayScheduler scheduler))
                        {
                            schedulers.Remove(item);
                            scheduler.Dispose();
                        }
                    }
                }

                newDesignTimeInputs = designTimeInputs.ToImmutable();
                newSharedDesignTimeInputs = designTimeSharedInputs.ToImmutable();
                newCookies = cookies.ToImmutable();
                newSchedulers = schedulers.ToImmutable();
            }
            else
            {
                // If there haven't been file changes we can just flow our previous collections to the new version to avoid roundtriping our collections to builders and back
                newDesignTimeInputs = previousValue.Inputs;
                newSharedDesignTimeInputs = previousValue.SharedInputs;
                newCookies = previousValue.Cookies;
                newSchedulers = previousValue.TaskSchedulers;
            }

            // Apply our new value
            AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem
            {
                Inputs = newDesignTimeInputs,
                SharedInputs = newSharedDesignTimeInputs,
                // We always need an output path, so if it hasn't changed we just reuse the previous value
                OutputPath = value.OutputPath ?? previousValue.OutputPath,
                Cookies = newCookies,
                TaskSchedulers = newSchedulers
            }, value.DataSourceVersions);

            // Project properties changes cause all PEs to be dirty and recompile if this isn't the first update
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
                BuildManager.OnDesignTimeOutputDeleted(item);
            }

            static bool TryGetValueIfUnused<T>(string item, ImmutableDictionary<string, T>.Builder source, ImmutableHashSet<string>.Builder otherSources, out T result)
            {
                // return the value from the source, but only if it doesn't appear in other sources
                return source.TryGetValue(item, out result) && !otherSources.Contains(item);
            }

        }

        protected virtual ITaskDelayScheduler CreateTaskScheduler()
        {
            return new TaskDelayScheduler(s_compilationWaitTime, _unconfiguredProjectServices.ThreadingService, DisposalToken);
        }

        public Task LoadAsync()
        {
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            return DisposeAsync();
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            DisposeTaskSchedulers();
            await base.DisposeCoreAsync(initialized);
        }

        /// <summary>
        /// InitializeInnerCoreAsync is responsible for setting an initial AppliedValue. This value will be used by any UI thread calls that may happen
        /// before the first data flow blocks have been processed. If this method doesn't set a value then the system will block until the first blocks
        /// have been applied.
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
            JoinUpstreamDataSources(_projectSubscriptionService.SourceItemsRuleSource, _projectSubscriptionService.ProjectRuleSource);

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
            static (bool designTime, bool designTimeShared) GetDesignTimePropsForItem(IImmutableDictionary<string, string> item)
            {
                item.TryGetValue(Compile.LinkProperty, out string linkString);
                item.TryGetValue(Compile.DesignTimeProperty, out string designTimeString);
                item.TryGetValue(Compile.DesignTimeSharedInputProperty, out string designTimeSharedString);

                if (linkString != null && linkString.Length > 0)
                {
                    // Linked files are never used as TempPE inputs
                    return (false, false);
                }

                return (StringComparers.PropertyLiteralValues.Equals(designTimeString, bool.TrueString), StringComparers.PropertyLiteralValues.Equals(designTimeSharedString, bool.TrueString));
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

        private void DisposeTaskSchedulers()
        {
            DesignTimeInputsItem value = AppliedValue?.Value;

            if (value == null) return;

            foreach (ITaskDelayScheduler scheduler in value.TaskSchedulers.Values)
            {
                scheduler.Dispose();
            }
        }
    }
}
