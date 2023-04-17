// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    [Export(typeof(IWebWorkloadDescriptorDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class WebWorkloadDescriptorDataSource : ChainedProjectValueDataSourceBase<ISet<WorkloadDescriptor>>, IWebWorkloadDescriptorDataSource
    {
        private static readonly WorkloadDescriptor s_webWorkload = new("Web", "Microsoft.VisualStudio.Component.Web");

        private readonly ConfiguredProject _configuredProject;

        private bool _haveReportedWebWorkload;

        [ImportingConstructor]
        public WebWorkloadDescriptorDataSource(ConfiguredProject configuredProject)
            : base(configuredProject)
        {
            _configuredProject = configuredProject;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ISet<WorkloadDescriptor>>> targetBlock)
        {
            IReceivableSourceBlock<IProjectVersionedValue<IProjectCapabilitiesSnapshot>> sourceBlock = _configuredProject.Capabilities.SourceBlock;

            DisposableValue<ISourceBlock<IProjectVersionedValue<ISet<WorkloadDescriptor>>>> transformBlock = sourceBlock.TransformWithNoDelta(u => u.Derive(CreateWorkloadDescriptor));

            return new DisposableBag
            {
                transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion),
                transformBlock,
                JoinUpstreamDataSources(_configuredProject.Capabilities)
            };
        }

        private ISet<WorkloadDescriptor> CreateWorkloadDescriptor(IProjectCapabilitiesSnapshot capabilities)
        {
            var workloads = ImmutableHashSet<WorkloadDescriptor>.Empty;

            // Note that we only report the web workload once. After that, we report an empty set.
            // Downstream components only need to be notified once, as they unsubscribe immediately when a missing workload is detected.
            // TODO revisit this pattern as it seems like we could use fewer dataflow blocks to achieve this, end-to-end.
            if (!_haveReportedWebWorkload && RequiresWebComponent())
            {
                workloads = workloads.Add(s_webWorkload);

                _haveReportedWebWorkload = true;
            }

            return workloads;

            bool RequiresWebComponent()
            {
                // DotNetRazor && (WindowsForms || WPF)
                return capabilities.IsProjectCapabilityPresent(ProjectCapability.DotNetRazor) &&
                    (capabilities.IsProjectCapabilityPresent(ProjectCapability.WindowsForms) || capabilities.IsProjectCapabilityPresent(ProjectCapability.WPF));
            }
        }
    }
}
