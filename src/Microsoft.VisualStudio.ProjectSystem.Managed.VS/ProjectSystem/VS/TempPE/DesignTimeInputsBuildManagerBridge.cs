// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(IDesignTimeInputsBuildManagerBridge))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class DesignTimeInputsBuildManagerBridge : UnconfiguredProjectHostBridge<IProjectVersionedValue<DesignTimeInputSnapshot>, IProjectVersionedValue<DesignTimeInputSnapshot>, IProjectVersionedValue<DesignTimeInputSnapshot>>, IDesignTimeInputsBuildManagerBridge
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

        public async Task<string[]> GetDesignTimeOutputMonikersAsync()
        {
            await InitializeAsync();

            Assumes.NotNull(AppliedValue);

            DesignTimeInputSnapshot value = AppliedValue.Value;

            return value.Inputs.Select(_project.MakeRelative).ToArray();
        }

        public async Task<string> BuildDesignTimeOutputAsync(string outputMoniker)
        {
            if (!SkipInitialization)
            {
                await InitializeAsync();
            }

            Assumes.NotNull(AppliedValue);

            DesignTimeInputSnapshot value = AppliedValue.Value;

            return string.IsNullOrEmpty(value.TempPEOutputPath) ? string.Empty :
                await _designTimeInputsCompiler.BuildDesignTimeOutputAsync(outputMoniker, value.TempPEOutputPath, value.SharedInputs);
        }

        /// <summary>
        /// ApplyAsync is called on the UI thread and its job is to update AppliedValue to be correct based on the changes that have come through data flow after being processed
        /// </summary>
        protected override async Task ApplyAsync(IProjectVersionedValue<DesignTimeInputSnapshot> value)
        {
            // Not using use the ThreadingService property because unit tests
            await JoinableFactory.SwitchToMainThreadAsync();

            IProjectVersionedValue<DesignTimeInputSnapshot>? previous = AppliedValue;

            AppliedValue = value;

            // To avoid callers seeing an inconsistent state where there are no monikers,
            // we use BlockInitializeOnFirstAppliedValue to block on the first value
            // being applied.
            //
            // Due to that, and to avoid a deadlock when event handlers call back into us
            // while we're still initializing, we avoid firing the events the first time 
            // a value is applied.
            if (previous is not null)
            {
                DesignTimeInputSnapshot currentValue = value.Value;
                DesignTimeInputSnapshot previousValue = previous.Value;

                foreach (DesignTimeInputFileChange change in currentValue.ChangedInputs)
                {
                    _buildManager.OnDesignTimeOutputDirty(_project.MakeRelative(change.File));
                }

                foreach (string item in previousValue.Inputs.Except(currentValue.Inputs))
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
        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<DesignTimeInputSnapshot>> targetBlock)
        {
            JoinUpstreamDataSources(_designTimeInputsChangeTracker);

            return _designTimeInputsChangeTracker.SourceBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion);
        }

        /// <summary>
        /// Preprocess gets called as each data flow block updates and its job is to take the input from those blocks and do whatever work needed
        /// so that ApplyAsync has all of the info it needs to do its job.
        /// </summary>
        protected override Task<IProjectVersionedValue<DesignTimeInputSnapshot>?> PreprocessAsync(IProjectVersionedValue<DesignTimeInputSnapshot> input, IProjectVersionedValue<DesignTimeInputSnapshot>? previousOutput)
        {
            // No need to manipulate the data
            return Task.FromResult<IProjectVersionedValue<DesignTimeInputSnapshot>?>(input);
        }
    }
}
