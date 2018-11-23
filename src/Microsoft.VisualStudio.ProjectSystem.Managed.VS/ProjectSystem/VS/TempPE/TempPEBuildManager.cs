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
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : UnconfiguredProjectHostBridge<
            /*  Input: */ IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>>,  //The snapshot that gets processed for later publishing to the UI thread
                                                                                                        /* Output: */ DesignTimeInputs,                                                             //The data that is used to apply changes to the UI thread
                                                                                                                                                                                                    /* Appled: */ IProjectVersionedValue<DesignTimeInputs>                                      //The snapshot that gets published to the UI thread.  This type should be immutable
        >, ITempPEBuildManager, IDisposable
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ILanguageServiceHost _languageServiceHost;
        //private readonly ITempPECompiler _compiler;
        private readonly CancellationSeries _cancellationSeries;

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

            _cancellationSeries = new CancellationSeries();
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _cancellationSeries.Dispose();

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

        public string[] GetDesignTimeOutputFilenames()
        {
            return AppliedValue?.Value.Inputs.Keys.ToArray();
        }

        public async Task<string> GetTempPEBlobAsync(string fileName)
        {
            if (AppliedValue == null || !AppliedValue.Value.SharedInputs.TryGetValue(fileName, out var series))
            {
                return null; // TODO: Is this right? Do we want an empty string? Or should it return the last good XML?
            }

            var token = series.Item2.CreateNext();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread(token);

            token.ThrowIfCancellationRequested();

            var files = new HashSet<string>(AppliedValue?.Value.SharedInputs.Keys, StringComparers.Paths);
            files.Add(series.Item1);

            var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfiguredBrowseObjectPropertiesAsync();

            var objPath = await property.IntermediatePath.GetValueAsPathAsync(false, false);
            var basePath = await property.FullPath.GetValueAsPathAsync(false, false);
            var outputFileName = fileName + ".dll";
            var outputPath = Path.Combine(basePath, objPath, "TempPE");

            // var result = await _compiler.CompileAsync(_languageServiceHost.ActiveProjectContext, Path.Combine(outputPath, outputFileName), files, token);
            //
            // if (!result)
            // {
            //     return null; // TODO: Is this right? Do we want an empty string? Or should it return the last good XML?
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

        protected override Task ApplyAsync(DesignTimeInputs value)
        {
            AppliedValue = new ProjectVersionedValue<DesignTimeInputs>(value, value.DataSourceVersions);

            return Task.CompletedTask;
        }

        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken)
        {
            AppliedValue = default;

            return Task.CompletedTask;
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>>> targetBlock)
        {
            return ProjectDataSources.SyncLinkTo(
                ProjectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                ProjectSubscriptionService.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                cancellationToken: ProjectAsynchronousTasksService.UnloadCancellationToken);
        }

        protected override Task<DesignTimeInputs> PreprocessAsync(IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>> input, DesignTimeInputs previousOutput)
        {
            var project = input.Value.Item1.ProjectInstance;
            var changes = input.Value.Item2.ProjectChanges[Compile.SchemaName];

            var designTimeInputs = AppliedValue?.Value.Inputs.ToBuilder() ?? ImmutableDictionary.CreateBuilder<string, (string, CancellationSeries)>(StringComparers.Paths);
            var designTimeSharedInputs = AppliedValue?.Value.SharedInputs.ToBuilder() ?? ImmutableDictionary.CreateBuilder<string, (string, CancellationSeries)>(StringComparers.Paths);

            foreach (var item in changes.Difference.AddedItems)
            {
                var projItem = project.GetItemsByItemTypeAndEvaluatedInclude(Compile.SchemaName, item).First();
                bool link = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.LinkProperty), bool.TrueString);
                if (!link)
                {
                    bool designTime = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.DesignTimeProperty), bool.TrueString);
                    bool designTimeShared = StringComparers.PropertyValues.Equals(projItem.GetMetadataValue(Compile.DesignTimeSharedInputProperty), bool.TrueString);

                    if (designTime)
                    {
                        designTimeInputs.Add(item, (projItem.GetMetadataValue(Compile.FullPathProperty), new CancellationSeries()));
                    }
                    else if (designTimeShared)
                    {
                        designTimeSharedInputs.Add(item, (projItem.GetMetadataValue(Compile.FullPathProperty), new CancellationSeries()));
                    }
                }
            }

            foreach (var item in changes.Difference.RemovedItems)
            {
                if (designTimeInputs.TryGetValue(item, out var series))
                {
                    series.Item2?.Dispose();
                    designTimeInputs.Remove(item);
                }
                if (designTimeSharedInputs.TryGetValue(item, out var series2))
                {
                    series2.Item2?.Dispose();
                    designTimeSharedInputs.Remove(item);
                }
            }

            var result = new DesignTimeInputs
            {
                Inputs = designTimeInputs.ToImmutable(),
                SharedInputs = designTimeSharedInputs.ToImmutable(),
                DataSourceVersions = input.DataSourceVersions
            };

            return Task.FromResult(result);
        }
    }

    internal class DesignTimeInputs
    {
        public ImmutableDictionary<string, (string, CancellationSeries)> Inputs { get; set; }

        public ImmutableDictionary<string, (string, CancellationSeries)> SharedInputs { get; set; }

        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; set; }
    }
}
