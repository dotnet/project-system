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
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.Threading.Tasks;

using InputTuple = System.Tuple<Microsoft.VisualStudio.ProjectSystem.IProjectSnapshot, Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : UnconfiguredProjectHostBridge<IProjectVersionedValue<InputTuple>, DesignTimeInputsDelta, IProjectVersionedValue<DesignTimeInputsItem>>, ITempPEBuildManager, IDisposable
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly ITempPECompiler _compiler;

        // for testing
        protected VSBuildManager _buildManager;

        [ImportingConstructor]
        public TempPEBuildManager(IProjectThreadingService threadingService,
            IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ILanguageServiceHost languageServiceHost,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            VSLangProj.VSProjectEvents projectEvents,
            ITempPECompiler compiler
            )
             : base(threadingService.JoinableTaskContext)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _languageServiceHost = languageServiceHost;
            _projectSubscriptionService = projectSubscriptionService;
            _compiler = compiler;
            _buildManager = (VSBuildManager)projectEvents?.BuildManagerEvents;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (AppliedValue != null)
            {
                // dispose any of our cancellation series we still have
                foreach (CancellationSeries item in AppliedValue.Value.Inputs.Values)
                {
                    item?.Dispose();
                }
            }

            return base.DisposeCoreAsync(initialized);
        }

        [ProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        public Task OnProjectLoaded()
        {
            return InitializeAsync();
        }

        public string[] GetTempPESourceFileNames()
        {
            return AppliedValue.Value.Inputs.Keys.ToArray();
        }

        public void TryFireTempPEDirty(string fileName)
        {
            _unconfiguredProjectServices.ThreadingService.VerifyOnUIThread();

            DesignTimeInputsItem inputs = AppliedValue.Value;
            if (inputs.SharedInputs.Contains(fileName))
            {
                foreach (string item in inputs.Inputs.Keys)
                {
                    _buildManager.OnDesignTimeOutputDirty(item);
                }
            }
            else if (inputs.Inputs.ContainsKey(fileName))
            {
                _buildManager.OnDesignTimeOutputDirty(fileName);
            }
        }

        public async Task<string> GetTempPEDescriptionXmlAsync(string fileName)
        {
            DesignTimeInputsItem inputs = AppliedValue.Value;
            if (fileName == null)
                throw new ArgumentException("Must supply a file to build", nameof(fileName));

            if (!inputs.Inputs.TryGetValue(fileName, out CancellationSeries cancellation))
                throw new ArgumentException("FileName supplied must be one of the DesignTime source files", nameof(fileName));

            // getting the next token will automatically cancel any current compilation that is already happening for this file
            CancellationToken token = cancellation.CreateNext();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread(token);

            token.ThrowIfCancellationRequested();

            ConfiguredBrowseObject browseObject = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfiguredBrowseObjectPropertiesAsync();
            string basePath = await browseObject.FullPath.GetValueAsPathAsync(false, false);
            string objPath = await browseObject.IntermediatePath.GetValueAsPathAsync(false, false);
            string outputPath = Path.Combine(basePath, objPath, "TempPE");

            var files = new HashSet<string>(inputs.SharedInputs.Count + 1, StringComparers.Paths);
            files.AddRange(inputs.SharedInputs.Select(UnconfiguredProject.MakeRooted));
            files.Add(UnconfiguredProject.MakeRooted(fileName));

            string outputFileName = fileName.Replace('\\', '.') + ".dll";

            var result = await _compiler.CompileAsync(_languageServiceHost.ActiveProjectContext, Path.Combine(outputPath, outputFileName), files, token);
            if (!result)
            {
                // if the compilation failed we should clean up any old TempPE outputs lest a designer gets the wrong types
                try
                {
                    if (File.Exists(outputFileName))
                    {
                        File.Delete(outputFileName);
                    }
                }
                catch (IOException)
                { }
                return null;
            }

            // VSTypeResolutionService is the only consumer, and it only uses the codebase element so just default most of them
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

        protected override Task ApplyAsync(DesignTimeInputsDelta value)
        {
            // Not using use the ThreadingService property because unit tests
            _unconfiguredProjectServices.ThreadingService.VerifyOnUIThread();

            DesignTimeInputsItem applied = AppliedValue.Value;
            var designTimeInputs = applied.Inputs.ToBuilder();
            var designTimeSharedInputs = applied.SharedInputs.ToBuilder();

            var addedDesignTimeInputs = new List<string>();
            var removedDesignTimeInputs = new List<string>();

            foreach (string item in value.AddedItems)
            {
                // If a property changes we might be asked to add items that already exist, so we just ignore them as we're happy using our existing CancellationSeries
                if (!designTimeInputs.ContainsKey(item))
                {
                    designTimeInputs.Add(item, new CancellationSeries());
                    addedDesignTimeInputs.Add(item);
                }
            }

            foreach (string item in value.AddedSharedItems)
            {
                designTimeSharedInputs.Add(item);
            }

            foreach (string item in value.RemovedItems)
            {
                if (designTimeInputs.TryGetValue(item, out CancellationSeries series))
                {
                    series?.Dispose();
                    designTimeInputs.Remove(item);
                    removedDesignTimeInputs.Add(item);
                }
                designTimeSharedInputs.Remove(item);
            }

            AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem
            {
                Inputs = designTimeInputs.ToImmutable(),
                SharedInputs = designTimeSharedInputs.ToImmutable(),
            }, value.DataSourceVersions);

            // Fire off the events
            if (value.AddedSharedItems.Count > 0)
            {
                // if the shared items changed then all TempPEs are dirty, because shared items are included in all TempPEs
                foreach (string item in designTimeInputs.Keys)
                {
                    _buildManager.OnDesignTimeOutputDirty(item);
                }
            }
            else
            {
                // otherwise just the ones that we've added are dirty
                foreach (string item in addedDesignTimeInputs)
                {
                    _buildManager.OnDesignTimeOutputDirty(item);
                }
            }
            foreach (string item in removedDesignTimeInputs)
            {
                _buildManager.OnDesignTimeOutputDeleted(item);
            }

            return Task.CompletedTask;
        }

        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken)
        {
            AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem(), ImmutableDictionary.Create<NamedIdentity, IComparable>());

            return Task.CompletedTask;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<InputTuple>> targetBlock)
        {
            return ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                _projectSubscriptionService.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                cancellationToken: ProjectAsynchronousTasksService.UnloadCancellationToken);
        }

        protected override Task<DesignTimeInputsDelta> PreprocessAsync(IProjectVersionedValue<InputTuple> input, DesignTimeInputsDelta previousOutput)
        {
            ProjectInstance project = input.Value.Item1.ProjectInstance;
            IProjectChangeDescription changes = input.Value.Item2.ProjectChanges[Compile.SchemaName];

            ImmutableArray<string>.Builder addedDesignTimeInputs = ImmutableArray.CreateBuilder<string>();
            ImmutableArray<string>.Builder removedDesignTimeInputs = ImmutableArray.CreateBuilder<string>();
            var addedDesignTimeSharedInputs = AppliedValue.Value.SharedInputs.ToBuilder();

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

            var result = new DesignTimeInputsDelta
            {
                AddedItems = addedDesignTimeInputs.ToImmutable(),
                RemovedItems = removedDesignTimeInputs.ToImmutable(),
                AddedSharedItems = addedDesignTimeSharedInputs.ToImmutable(),
                DataSourceVersions = input.DataSourceVersions
            };

            return Task.FromResult(result);

            bool PreprocessAddItem(string item)
            {
                ProjectItemInstance projItem = project.GetItemsByItemTypeAndEvaluatedInclude(Compile.SchemaName, item).FirstOrDefault();
                System.Diagnostics.Debug.Assert(projItem != null, "Couldn't find the project item for Compile with an evaluated include of " + item);

                bool link = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.LinkProperty), bool.TrueString);
                if (!link)
                {
                    bool designTime = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.DesignTimeProperty), bool.TrueString);
                    bool designTimeShared = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.DesignTimeSharedInputProperty), bool.TrueString);

                    if (designTime)
                    {
                        addedDesignTimeInputs.Add(item);
                        return true;
                    }
                    else if (designTimeShared)
                    {
                        addedDesignTimeSharedInputs.Add(item);
                        return true;
                    }
                }
                return false;
            }
        }

        protected override bool ShouldValueBeApplied(DesignTimeInputsDelta previouslyAppliedOutput, DesignTimeInputsDelta newOutput)
        {
            return newOutput.AddedItems.Length > 0 ||
                   newOutput.AddedSharedItems.Count > 0 ||
                   newOutput.RemovedItems.Length > 0;
        }
    }

    internal class DesignTimeInputsItem
    {
        public ImmutableDictionary<string, CancellationSeries> Inputs { get; set; } = ImmutableDictionary.Create<string, CancellationSeries>(StringComparers.Paths);
        public ImmutableHashSet<string> SharedInputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
    }

    internal class DesignTimeInputsDelta
    {
        public ImmutableHashSet<string> AddedSharedItems { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
        public ImmutableArray<string> AddedItems { get; set; } = ImmutableArray.Create<string>();
        public ImmutableArray<string> RemovedItems { get; set; } = ImmutableArray.Create<string>();
        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; set; } = ImmutableSortedDictionary.Create<NamedIdentity, IComparable>();
    }
}
