// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
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
        private readonly IWebWorkloadDescriptorDataSource _webWorkloadDescriptorDataSource;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;

        private bool _enabled;
        private Guid _projectGuid;
        private DisposableBag? _subscription;

        [ImportingConstructor]
        public WebMissingWorkloadDetector(
            ConfiguredProject project,
            IWebWorkloadDescriptorDataSource webWorkloadDescriptorDataSource,
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _webWorkloadDescriptorDataSource = webWorkloadDescriptorDataSource;
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _projectFaultHandlerService = projectFaultHandlerService;
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

            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Note we don't use the ISafeProjectGuidService here because it is generally *not*
            // safe to use within IProjectDynamicLoadComponent.LoadAsync.
            _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();

            Action<IProjectVersionedValue<ISet<WorkloadDescriptor>>> action = OnWorkloadDescriptorsComputed;

            _subscription = new()
            {
                _webWorkloadDescriptorDataSource.SourceBlock.LinkTo(
                    DataflowBlockFactory.CreateActionBlock(action, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality, nameFormat: $"{nameof(WebMissingWorkloadDetector)} Action: {{1}}"),
                    linkOptions: DataflowOption.PropagateCompletion),

                ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _webWorkloadDescriptorDataSource)
            };
        }

        private void OnWorkloadDescriptorsComputed(IProjectVersionedValue<ISet<WorkloadDescriptor>> update)
        {
            if (_enabled && update.Value.Count != 0)
            {
                _missingSetupComponentRegistrationService.RegisterMissingWebWorkloads(_projectGuid, _project, update.Value);
            }
        }
    }
}
