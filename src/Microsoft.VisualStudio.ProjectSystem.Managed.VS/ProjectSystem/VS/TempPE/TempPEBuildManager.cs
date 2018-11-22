// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : UnconfiguredProjectHostBridge<
            /*  Input: */ IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>>,
            /* Output: */ IProjectVersionedValue<DesignTimeInputs>,
            /* Appled: */ IProjectVersionedValue<DesignTimeInputs>
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

        public string[] GetDesignTimeOutputFilenames()
        {
            return AppliedValue?.Value.Inputs;
        }

        public async Task<string> GetTempPEBlobAsync(string fileName)
        {
            CancellationToken token = _cancellationSeries.CreateNext();

            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread(token);

            token.ThrowIfCancellationRequested();

            var files = new HashSet<string>(AppliedValue?.Value.SharedInputs, StringComparers.Paths);
            files.Add(fileName);

            var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfiguredBrowseObjectPropertiesAsync();

            var objPath = await property.IntermediatePath.GetValueAsPathAsync(false, false);
            var basePath = await property.FullPath.GetValueAsPathAsync(false, false);
            var inputFileName = Path.GetFileName(fileName);
            var outputFileName = inputFileName + ".dll";
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
    name = ""{inputFileName}""  
    version = ""0.0.0.0""  
    snapshot_id = ""1""
    replaceable = ""True""  
  />  
</root>";
        }

        protected override Task ApplyAsync(IProjectVersionedValue<DesignTimeInputs> value)
        {
            AppliedValue = value;

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

        protected override Task<IProjectVersionedValue<DesignTimeInputs>> PreprocessAsync(IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>> input, IProjectVersionedValue<DesignTimeInputs> previousOutput)
        {
            var compileItems = input.Value.Item1.ProjectInstance.GetItems(Compile.SchemaName);

            var designTimeInputs = new List<string>();
            var sharedDesignTimeInputs = new List<string>();
            foreach (var item in compileItems)
            {
                bool link = StringComparers.PropertyValues.Equals(item.GetMetadataValue(Compile.LinkProperty), bool.TrueString);
                if (!link)
                {
                    bool designTime = StringComparers.PropertyValues.Equals(item.GetMetadataValue(Compile.DesignTimeProperty), bool.TrueString);
                    bool designTimeShared = StringComparers.PropertyValues.Equals(item.GetMetadataValue(Compile.DesignTimeSharedInputProperty), bool.TrueString);
                    if (designTime)
                    {
                        designTimeInputs.Add(item.GetMetadataValue(Compile.FullPathProperty));
                    }
                    else if (designTimeShared)
                    {
                        sharedDesignTimeInputs.Add(item.GetMetadataValue(Compile.FullPathProperty));
                    }
                }
            }

            var result = new ProjectVersionedValue<DesignTimeInputs>(new DesignTimeInputs
            {
                Inputs = designTimeInputs.ToArray(),
                SharedInputs = sharedDesignTimeInputs.ToArray()
            }, input.DataSourceVersions);

            return Task.FromResult((IProjectVersionedValue<DesignTimeInputs>)result);
        }
    }

    internal class DesignTimeInputs
    {
        public string[] Inputs { get; set; }
        public string[] SharedInputs { get; set; }
    }
}
