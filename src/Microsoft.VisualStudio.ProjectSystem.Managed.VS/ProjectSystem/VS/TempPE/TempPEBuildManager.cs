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
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.Threading.Tasks;
using VSLangProj;
using InputTuple = System.Tuple<Microsoft.VisualStudio.ProjectSystem.IProjectSnapshot, Microsoft.VisualStudio.ProjectSystem.IProjectSubscriptionUpdate>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : UnconfiguredProjectHostBridge<IProjectVersionedValue<InputTuple>, DesignTimeInputsDelta, IProjectVersionedValue<DesignTimeInputsItem>>, ITempPEBuildManager, IDisposable
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ILanguageServiceHost _languageServiceHost;
        //private readonly ITempPECompiler _compiler;

        [ImportingConstructor]
        public TempPEBuildManager(IProjectThreadingService threadingService,
            IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ILanguageServiceHost languageServiceHost
            //,ITempPECompilerHost compilerHost
            )
             : base(threadingService.JoinableTaskContext)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _languageServiceHost = languageServiceHost;
            //_compiler = compiler;
            BuildManager = new OrderPrecedenceImportCollection<BuildManager>(projectCapabilityCheckProvider: _unconfiguredProjectServices.Project);
        }

        [ImportMany(typeof(BuildManager))]
        internal OrderPrecedenceImportCollection<BuildManager> BuildManager { get; set; }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (AppliedValue != null)
            {
                // dispose any of our cancellation series we still have
                foreach (var item in AppliedValue.Value.Inputs.Values)
                {
                    item?.Dispose();
                }
            }

            return base.DisposeCoreAsync(initialized);
        }

        /// <summary>
        /// Use the project subscription service to read connected services data from the tree service.
        /// </summary>
        [Import]
        private IActiveConfiguredProjectSubscriptionService ProjectSubscriptionService { get; set; }

        [ProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        public Task OnProjectLoaded()
        {
            return InitializeAsync();
        }

        public async Task<string[]> GetDesignTimeOutputFilenamesAsync()
        {
            var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfiguredBrowseObjectPropertiesAsync();
            var basePath = await property.FullPath.GetValueAsPathAsync(false, false);
            return AppliedValue.Value.Inputs.Keys.Select(i => Path.Combine(basePath, i)).ToArray();
        }

        public async Task<string> GetTempPEBlobAsync(string fileName)
        {
            var inputs = AppliedValue.Value;
            if (fileName == null || inputs.Inputs.TryGetValue(fileName, out var cancellation))
            {
                return null;
            }

            // getting the next token will automatically cancel any current compilation that is already happening for this file
            var token = cancellation.CreateNext();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread(token);

            token.ThrowIfCancellationRequested();

            var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfiguredBrowseObjectPropertiesAsync();
            var basePath = await property.FullPath.GetValueAsPathAsync(false, false);
            var objPath = await property.IntermediatePath.GetValueAsPathAsync(false, false);
            var outputPath = Path.Combine(basePath, objPath, "TempPE");

            var files = new HashSet<string>(inputs.SharedInputs.Count + 1, StringComparers.Paths);
            files.AddRange(inputs.SharedInputs.Select(i => Path.Combine(basePath, i)));
            files.Add(Path.Combine(basePath, fileName));

            var outputFileName = fileName + ".dll";

            // var result = await _compiler.CompileAsync(_languageServiceHost.ActiveProjectContext, Path.Combine(outputPath, outputFileName), files, token);
            //
            // if (!result)
            // {
            //     return null;
            // }

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
            var applied = AppliedValue.Value;
            var designTimeInputs = applied.Inputs.ToBuilder();
            var designTimeSharedInputs = applied.SharedInputs.ToBuilder();

            var addedDesignTimeInputs = new List<string>();
            var removedDesignTimeInputs = new List<string>();

            foreach (var item in value.AddedItems)
            {
                // If a property changes we might be asked to add items that already exist, so we just ignore them as we're happy using our existing CancellationSeries
                if (!designTimeInputs.ContainsKey(item))
                {
                    designTimeInputs.Add(item, new CancellationSeries());
                    addedDesignTimeInputs.Add(item);
                }
            }

            foreach (var item in value.AddedSharedItems)
            {
                designTimeSharedInputs.Add(item);
            }

            foreach (var item in value.RemovedItems)
            {
                if (designTimeInputs.TryGetValue(item, out var series))
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
            var buildManager = BuildManager.First().Value as VSBuildManager;
            if (value.AddedSharedItems.Count > 0)
            {
                // if the shared items changed then all TempPEs are dirty, because shared items are included in all TempPEs
                foreach (var item in designTimeInputs.Keys)
                {
                    buildManager.OnDesignTimeOutputDirty(item);
                }
            }
            else
            {
                // otherwise just the ones that we've added are dirty
                foreach (var item in addedDesignTimeInputs)
                {
                    buildManager.OnDesignTimeOutputDirty(item);
                }
            }
            foreach (var item in removedDesignTimeInputs)
            {
                buildManager.OnDesignTimeOutputDeleted(item);
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
                ProjectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                ProjectSubscriptionService.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                cancellationToken: ProjectAsynchronousTasksService.UnloadCancellationToken);
        }

        protected override Task<DesignTimeInputsDelta> PreprocessAsync(IProjectVersionedValue<InputTuple> input, DesignTimeInputsDelta previousOutput)
        {
            var project = input.Value.Item1.ProjectInstance;
            var changes = input.Value.Item2.ProjectChanges[Compile.SchemaName];
            
            var addedDesignTimeInputs = ImmutableList.CreateBuilder<string>();
            var removedDesignTimeInputs = ImmutableList.CreateBuilder<string>();
            var addedDesignTimeSharedInputs = AppliedValue.Value.SharedInputs.ToBuilder();

            foreach (var item in changes.Difference.AddedItems)
            {
                PreprocessAddItem(item);
            }

            foreach (var item in changes.Difference.RemovedItems)
            {
                // the RemovedItems doesn't have any project info any more so we can't tell if an item was a design time input or not
                // and we can't check in AppliedValue because there could be an unapplied value that adds an item
                // so we have to just process the removed items in ApplyAsync and take advantage of the fact that its
                // quick to remove from immutable collections, even if the item doesn't exist, because they're implemented as trees
                removedDesignTimeInputs.Add(item);
            }

            foreach (var item in changes.Difference.RenamedItems)
            {
                // a rename is just an add and a remove. Key is the old name, Value is the new name
                PreprocessAddItem(item.Value);
                removedDesignTimeInputs.Add(item.Key);
            }

            foreach (var item in changes.Difference.ChangedItems)
            {
                // Items appear in this set if, for example, DesignTime goes from false to true, but we don't know which property changed, or what the old value was
                // So the logic is this:
                // Previous Design Time   | New Design Time   | Action
                // False                  | False             | PreprocessAddItem will return false, ApplyAsync will process remove, which is fast enough to not worry about
                // False                  | True              | PreprocessAddItem will return true, ApplyAsync will process as an add, so all good
                // True                   | False             | PreprocessAddItem will return false, ApplyAsync will process remove, so all good
                // True                   | True              | PreprocessAddItem will return true, ApplyAsync will not process add because key already exists
                if (!PreprocessAddItem(item))
                {
                    removedDesignTimeInputs.Add(item);
                }
            }

            foreach (var item in changes.Difference.ChangedProperties)
            {
                // TODO: Why do items appear here?
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
                var projItem = project.GetItemsByItemTypeAndEvaluatedInclude(Compile.SchemaName, item).FirstOrDefault();
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
            return newOutput.AddedItems.Count > 0 ||
                   newOutput.AddedSharedItems.Count > 0 ||
                   newOutput.RemovedItems.Count > 0;
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
        public ImmutableList<string> AddedItems { get; set; } = ImmutableList.Create<string>();
        public ImmutableList<string> RemovedItems { get; set; } = ImmutableList.Create<string>();
        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; set; } = ImmutableSortedDictionary.Create<NamedIdentity, IComparable>();
    }
}
