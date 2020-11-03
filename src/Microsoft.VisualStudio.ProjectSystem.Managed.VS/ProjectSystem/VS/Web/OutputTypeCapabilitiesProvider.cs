// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;

using ProjectCapabilitiesProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.IProjectCapabilitiesSnapshot>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    ///     Produces capabilities from output types.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class OutputTypeCapabilitiesProvider : ChainedProjectValueDataSourceBase<IProjectCapabilitiesSnapshot>, IProjectCapabilitiesProvider
    {
        private readonly IProjectSnapshotService _snapshotService;

        [ImportingConstructor]
        protected OutputTypeCapabilitiesProvider(ConfiguredProject configuredProject, IProjectSnapshotService snapshotService)
            : base(configuredProject)
        {
            _snapshotService = snapshotService;
        }

        public ProjectCapabilitiesProjectValue? Current { get; private set; }

        protected override IDisposable? LinkExternalInput(ITargetBlock<ProjectCapabilitiesProjectValue> targetBlock)
        {
            JoinUpstreamDataSources(_snapshotService);

            DisposableValue<ISourceBlock<ProjectCapabilitiesProjectValue>> block =
                _snapshotService.SourceBlock.Transform(
                    snapshot => snapshot.Derive(CreateCapabilitiesSnapshot));

            block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return block;
        }

        private IProjectCapabilitiesSnapshot CreateCapabilitiesSnapshot(IProjectSnapshot snapshot)
        {
            ImmutableHashSet<string> capabilities = Empty.CapabilitiesSet;

            string propertyValue = snapshot.ProjectInstance.GetPropertyValue(ConfigurationGeneral.OutputTypeProperty);
            
            switch (propertyValue)
            {
                case ConfigurationGeneral.OutputTypeValues.WinExe:
                    capabilities = capabilities.Add("WindowsExe");
                    break;

                case ConfigurationGeneral.OutputTypeValues.Library:
                    capabilities = capabilities.Add("Library");
                    break;

                case ConfigurationGeneral.OutputTypeValues.Exe:
                    capabilities = capabilities.Add("ConsoleExe");
                    break;
            }

            return Current == null
                ? new ProjectCapabilitiesSnapshot(capabilities)
                : ((ProjectCapabilitiesSnapshot)Current.Value).Update(capabilities);
        }
    }
}
