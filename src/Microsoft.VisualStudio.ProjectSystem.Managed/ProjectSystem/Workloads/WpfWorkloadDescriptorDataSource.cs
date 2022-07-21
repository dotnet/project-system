// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    [Export(typeof(IWpfWorkloadDescriptorDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class WpfWorkloadDescriptorDataSource : ChainedProjectValueDataSourceBase<ISet<WorkloadDescriptor>>, IWpfWorkloadDescriptorDataSource
    {
        private static readonly string s_webWorkloadName = "Web";
        private static readonly string s_webComponentId = "Microsoft.VisualStudio.Component.Web";
        private static readonly WorkloadDescriptor s_webWorkload = new(s_webWorkloadName, s_webComponentId);

        private readonly ConcurrentHashSet<string> _componentIdsDetected = new();
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public WpfWorkloadDescriptorDataSource(ConfiguredProject configuredProject)
            : base(configuredProject)
        {
            _configuredProject = configuredProject;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ISet<WorkloadDescriptor>>> targetBlock)
        {
            IReceivableSourceBlock<IProjectVersionedValue<IProjectCapabilitiesSnapshot>> sourceBlock = _configuredProject.Capabilities.SourceBlock;

            DisposableValue<ISourceBlock<IProjectVersionedValue<ISet<WorkloadDescriptor>>>>? transformBlock = sourceBlock.TransformWithNoDelta(transform => transform.Derive(u => CreateWorkloadDescriptor(u)));

            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return transformBlock;
        }

        private ISet<WorkloadDescriptor> CreateWorkloadDescriptor(IProjectCapabilitiesSnapshot projectCapabilitiesSnapshot)
        {
            var Workloads = ImmutableHashSet<WorkloadDescriptor>.Empty;

            bool dotnetCoreRazor = projectCapabilitiesSnapshot.IsProjectCapabilityPresent(ProjectCapability.DotNetRazor);
            bool windowsForm = projectCapabilitiesSnapshot.IsProjectCapabilityPresent(ProjectCapability.WindowsForms);
            bool wpf = projectCapabilitiesSnapshot.IsProjectCapabilityPresent(ProjectCapability.WPF);

            // Detect all possible scenarios and add the corresponding needed component ids.
            if (!_componentIdsDetected.Contains(s_webComponentId) && WpfDetected(dotnetCoreRazor, windowsForm, wpf))
            {
                Workloads = Workloads.Add(s_webWorkload);

                _componentIdsDetected.Add(s_webComponentId);
            }

            return Workloads;
        }

        #region Scenarios to detect

        private static bool WpfDetected(bool dotnetCoreRazor, bool windowsForm, bool wpf) => dotnetCoreRazor && (windowsForm || wpf);

        #endregion
    }
}
