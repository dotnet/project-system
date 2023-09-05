// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Workloads;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Workloads
{
    /// <summary>
    ///     Tracks the set of missing .NET workloads for a configured project.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingWorkloadDetector : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private readonly ConfiguredProject _project;
        private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;
        private readonly IWorkloadDescriptorDataSource _workloadDescriptorDataSource;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;

        private Guid _projectGuid;

        private IDisposable? _joinedDataSources;
        private IDisposable? _subscription;
        private bool? _hasNoMissingWorkloads;
        private ISet<WorkloadDescriptor>? _missingWorkloads;

        [ImportingConstructor]
        public MissingWorkloadDetector(
            ConfiguredProject project,
            IWorkloadDescriptorDataSource workloadDescriptorDataSource,
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _workloadDescriptorDataSource = workloadDescriptorDataSource;
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _projectFaultHandlerService = projectFaultHandlerService;
        }

        public Task LoadAsync()
        {
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _missingSetupComponentRegistrationService.UnregisterProjectConfiguration(_projectGuid, _project);

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _subscription?.Dispose();
            _joinedDataSources?.Dispose();

            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Note we don't use the ISafeProjectGuidService here because it is generally *not*
            // safe to use within IProjectDynamicLoadComponent.LoadAsync.
            _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();
            _joinedDataSources = ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _workloadDescriptorDataSource);

            _missingSetupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project);

            Action<IProjectVersionedValue<ISet<WorkloadDescriptor>>> action = OnWorkloadDescriptorsComputed;

            _subscription = _workloadDescriptorDataSource.SourceBlock.LinkTo(
                DataflowBlockFactory.CreateActionBlock(action, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
                linkOptions: DataflowOption.PropagateCompletion);
        }

        private void OnWorkloadDescriptorsComputed(IProjectVersionedValue<ISet<WorkloadDescriptor>> workloadDescriptors)
        {
            if (_hasNoMissingWorkloads == true)
            {
                return;
            }

            if (workloadDescriptors.Value.Count == 0)
            {
                _hasNoMissingWorkloads = true;
            }
            else
            {
                _missingWorkloads ??= new HashSet<WorkloadDescriptor>();
                if (!_missingWorkloads.AddRange(workloadDescriptors.Value))
                {
                    return;
                }
            }

            _missingSetupComponentRegistrationService.RegisterMissingWorkloads(_projectGuid, _project, workloadDescriptors.Value);
        }
    }
}
