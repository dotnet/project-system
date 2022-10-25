// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    /// <summary>
    ///     Tracks the set of missing .NET workloads for a configured project.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.RazorAndEitherWinFormsOrWpf)]
    internal class WebMissingWorkloadDetector : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private readonly ConfiguredProject _project;
        private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;
        private readonly IWebWorkloadDescriptorDataSource _wpfWorkloadDescriptorDataSource;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        private bool _enabled;
        private Guid _projectGuid;

        private IDisposable? _joinedDataSources;
        private IDisposable? _subscription;

        [ImportingConstructor]
        public WebMissingWorkloadDetector(
            ConfiguredProject project,
            IWebWorkloadDescriptorDataSource wpfWorkloadDescriptorDataSource,
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService,
            IProjectSubscriptionService projectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _wpfWorkloadDescriptorDataSource = wpfWorkloadDescriptorDataSource;
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _projectFaultHandlerService = projectFaultHandlerService;
            _projectSubscriptionService = projectSubscriptionService;
        }

        public Task LoadAsync()
        {
            _enabled = true;

            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _enabled = false;

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
            _joinedDataSources = ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _projectSubscriptionService.ProjectSource, _wpfWorkloadDescriptorDataSource);

            Action<IProjectVersionedValue<ValueTuple<IProjectSnapshot, ISet<WorkloadDescriptor>>>> action = OnWorkloadDescriptorsComputed;

            _subscription = ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                _wpfWorkloadDescriptorDataSource.SourceBlock.SyncLinkOptions(),
                    DataflowBlockFactory.CreateActionBlock(action, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
                    linkOptions: DataflowOption.PropagateCompletion,
                    cancellationToken: cancellationToken);
        }

        private void OnWorkloadDescriptorsComputed(IProjectVersionedValue<(IProjectSnapshot projectSnapshot, ISet<WorkloadDescriptor> workloadDescriptors)> pair)
        {
            if (!_enabled || pair.Value.workloadDescriptors.Count == 0)
            {
                return;
            }

            _missingSetupComponentRegistrationService.RegisterMissingWebWorkloads(_projectGuid, _project, pair.Value.workloadDescriptors);
        }
    }
}
