// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Runtimes
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
        private readonly ConfiguredProject _project;
        private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        private Guid _projectGuid;
        private IDisposable? _registration;
        private IDisposable? _subscription;

        [ImportingConstructor]
        public MissingSdkRuntimeDetector(
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            ConfiguredProject configuredProject,
            IProjectThreadingService threadingService,
            IProjectSubscriptionService projectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _project = configuredProject;
            _projectSubscriptionService = projectSubscriptionService;
        }

        public Task LoadAsync()
        {
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _registration?.Dispose();
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
            _registration = _missingSetupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project);
            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAction(
                target: OnProjectChanged, 
                project: _project.UnconfiguredProject,
                ruleNames: ConfigurationGeneral.SchemaName);
        }

        private void OnProjectChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            IImmutableDictionary<string, string> configurationGeneralProperties = update.Value.CurrentState[ConfigurationGeneral.SchemaName].Properties;
            if (!configurationGeneralProperties.TryGetValue(ConfigurationGeneral.TargetFrameworkIdentifierProperty, out string? targetFrameworkIdentifier) ||
                !configurationGeneralProperties.TryGetValue(ConfigurationGeneral.TargetFrameworkVersionProperty, out string? targetFrameworkVersion) || 
                targetFrameworkVersion is null)
            {
                return;
            }

            RegisterSdkNetCoreRuntimeNeededInProject(_project, targetFrameworkIdentifier, targetFrameworkVersion);
        }

        private void RegisterSdkNetCoreRuntimeNeededInProject(ConfiguredProject project, string? targetFrameworkIdentifier, string targetFrameworkVersion)
        {
            // set to empty for non-netcore projects so that we do not check for missing installed runtime for them
            if (!string.Equals(targetFrameworkIdentifier, TargetFrameworkIdentifiers.NetCoreApp, StringComparisons.FrameworkIdentifiers) ||
                string.IsNullOrEmpty(targetFrameworkVersion))
            {
                targetFrameworkVersion = string.Empty;
            }
            
            _missingSetupComponentRegistrationService.RegisterPossibleMissingSdkRuntimeVersion(_projectGuid, project, targetFrameworkVersion);
        }
    }
}
