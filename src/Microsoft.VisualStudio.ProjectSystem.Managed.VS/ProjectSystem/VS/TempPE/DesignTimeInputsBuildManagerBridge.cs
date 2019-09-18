// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(IDesignTimeInputsBuildManagerBridge))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class DesignTimeInputsBuildManagerBridge : UnconfiguredProjectHostBridge<IProjectVersionedValue<DesignTimeInputsDelta>, IProjectVersionedValue<DesignTimeInputsDelta>, IProjectVersionedValue<DesignTimeInputsDelta>>, IDesignTimeInputsBuildManagerBridge
    {
        private readonly UnconfiguredProject _project;
        private readonly IDesignTimeInputsChangeTracker _designTimeInputsChangeTracker;
        private readonly IDesignTimeInputsCompiler _designTimeInputsCompiler;
        private readonly VSBuildManager _buildManager;

        /// <summary>
        /// For unit testing purposes, to avoid having to mock all of CPS
        /// </summary>
        internal bool SkipInitialization { get; set; }

        [ImportingConstructor]
        public DesignTimeInputsBuildManagerBridge(UnconfiguredProject project,
                                                  IProjectThreadingService threadingService,
                                                  IDesignTimeInputsChangeTracker designTimeInputsChangeTracker,
                                                  IDesignTimeInputsCompiler designTimeInputsCompiler,
                                                  VSBuildManager buildManager)
             : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _designTimeInputsChangeTracker = designTimeInputsChangeTracker;
            _designTimeInputsCompiler = designTimeInputsCompiler;
            _buildManager = buildManager;
        }

        /// <summary>
        /// Get the list of design time monikers that need to have TempPE libraries created. Needs to be called on the UI thread.
        /// </summary>
        public async Task<string[]> GetTempPEMonikersAsync()
        {
            await InitializeAsync();

            DesignTimeInputsDelta value = AppliedValue.Value;

            return value.Inputs.Select(_project.MakeRelative).ToArray();
        }

        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        public async Task<string> GetDesignTimeInputXmlAsync(string relativeFileName)
        {
            if (!SkipInitialization)
            {
                await InitializeAsync();
            }

            DesignTimeInputsDelta value = AppliedValue.Value;

            return await _designTimeInputsCompiler.GetDesignTimeInputXmlAsync(relativeFileName, value.TempPEOutputPath, value.SharedInputs);
        }

        /// <summary>
        /// ApplyAsync is called on the UI thread and its job is to update AppliedValue to be correct based on the changes that have come through data flow after being processed
        /// </summary>
        protected override async Task ApplyAsync(IProjectVersionedValue<DesignTimeInputsDelta> value)
        {
            // Not using use the ThreadingService property because unit tests
            await JoinableFactory.SwitchToMainThreadAsync();

            DesignTimeInputsDelta delta = value.Value;

            ImmutableHashSet<string>? removedFiles = AppliedValue?.Value.Inputs.Except(delta.Inputs);

            // As it happens the DesignTimeInputsDelta contains all of the state we need
            AppliedValue = value;

            foreach (DesignTimeInputFileChange change in delta.ChangedInputs)
            {
                _buildManager.OnDesignTimeOutputDirty(_project.MakeRelative(change.File));
            }

            if (removedFiles != null)
            {
                foreach (string item in removedFiles)
                {
                    _buildManager.OnDesignTimeOutputDeleted(_project.MakeRelative(item));
                }
            }
        }

        /// <summary>
        /// InitializeInnerCoreAsync is responsible for setting an initial AppliedValue. This value will be used by any UI thread calls that may happen
        /// before the first data flow blocks have been processed. If this method doesn't set a value then the system will block until the first blocks
        /// have been applied.
        /// </summary>
        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is where we tell data flow which blocks we're interested in receiving updates for
        /// </summary>
        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<DesignTimeInputsDelta>> targetBlock)
        {
            JoinUpstreamDataSources(_designTimeInputsChangeTracker);

            return _designTimeInputsChangeTracker.SourceBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion);
        }

        /// <summary>
        /// Preprocess gets called as each data flow block updates and its job is to take the input from those blocks and do whatever work needed
        /// so that ApplyAsync has all of the info it needs to do its job.
        /// </summary>
        protected override Task<IProjectVersionedValue<DesignTimeInputsDelta>> PreprocessAsync(IProjectVersionedValue<DesignTimeInputsDelta> input, IProjectVersionedValue<DesignTimeInputsDelta> previousOutput)
        {
            // As it happens the DesignTimeInputsDelta contains all of the state we need
            return Task.FromResult(input);
        }
    }
}
