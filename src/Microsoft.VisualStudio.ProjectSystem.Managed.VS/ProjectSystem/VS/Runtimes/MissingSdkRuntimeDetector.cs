// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Detect during solution load the Net Core runtime version based on the target framework 
    /// used in a project file and pass this version to a service that can install the
    /// runtime component if not installed.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingSdkRuntimeDetector : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private static readonly string s_netCoreTargetFrameworkIdentifier = ".NETCoreApp";

        private Guid _projectGuid;

        private readonly ConfiguredProject _project;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;

        private IDisposable? _subscription;

        [ImportingConstructor]
        public MissingSdkRuntimeDetector(
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            ConfiguredProject configuredProject,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService, 
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _project = configuredProject;
            _projectFaultHandlerService = projectFaultHandlerService;
            _projectSubscriptionService = projectSubscriptionService;
        }
        
        
        private Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> _)
        {
            return RegisterSdkNetCoreRuntimeNeededInProjectAsync(_project);
        }

        public Task LoadAsync()
        {
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _missingSetupComponentRegistrationService.UnregisterProjectConfiguration(_projectGuid, _project);
            _subscription?.Dispose();
            
            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Note we don't use the ISafeProjectGuidService here because it is generally *not*
            // safe to use within IProjectDynamicLoadComponent.LoadAsync.
            _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();
            _missingSetupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project);
            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(target: OnProjectChangedAsync, _project.UnconfiguredProject);
        }

        private async Task RegisterSdkNetCoreRuntimeNeededInProjectAsync(ConfiguredProject project)
        {
            var projectProperties = project.Services.ExportProvider.GetExportedValue<ProjectProperties>();

            ConfigurationGeneral configuration = await projectProperties.GetConfigurationGeneralPropertiesAsync();

            string? targetFrameworkIdentifier = await configuration.TargetFrameworkIdentifier.GetDisplayValueAsync();

            string? targetFrameworkVersion = await configuration.TargetFrameworkVersion.GetDisplayValueAsync();

            if (!string.Equals(targetFrameworkIdentifier, s_netCoreTargetFrameworkIdentifier, StringComparisons.FrameworkIdentifiers) ||
                string.IsNullOrEmpty(targetFrameworkVersion))
            {
                targetFrameworkVersion = string.Empty;
            }
            
            _missingSetupComponentRegistrationService.RegisterPossibleMissingSdkRuntimeVersion(_projectGuid, project, targetFrameworkVersion);
        }
    }
}
