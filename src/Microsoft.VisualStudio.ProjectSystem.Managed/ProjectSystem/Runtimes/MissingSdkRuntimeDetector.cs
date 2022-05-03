// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingSdkRuntimeDetector : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private static readonly string s_netCoreTargetFrameworkIdentifier = ".NETCoreApp";

        private static readonly ImmutableDictionary<string, string> s_packageVersionToComponentId = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)
            .Add("v2.1", "Microsoft.Net.Core.Component.SDK.2.1")
            .Add("v3.1", "Microsoft.NetCore.Component.Runtime.3.1")
            .Add("v5.0", "Microsoft.NetCore.Component.Runtime.5.0");

        private Guid _projectGuid;
        private bool _enabled;

        private readonly ConfiguredProject _project;
        private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;

        [ImportingConstructor]
        public MissingSdkRuntimeDetector(
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            ConfiguredProject configuredProject,
            IProjectThreadingService threadingService)
            : base(threadingService.JoinableTaskContext)
        {
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _project = configuredProject;
        }

        public Task LoadAsync()
        {
            _enabled = true;
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _enabled = false;
            _missingSetupComponentRegistrationService.UnregisterProjectConfiguration(_projectGuid, _project);

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();
            _missingSetupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project);
            _ = RegisterSdkRuntimeNeededInProjectAsync(_project);
        }

        private async Task RegisterSdkRuntimeNeededInProjectAsync(ConfiguredProject project)
        {
            if (_enabled)
            {
                var projectProperties = project.Services.ExportProvider.GetExportedValue<ProjectProperties>();

                ConfigurationGeneral configuration = await projectProperties.GetConfigurationGeneralPropertiesAsync();

                string? TargetFrameworkIdentifier = await configuration.TargetFrameworkIdentifier.GetDisplayValueAsync();

                string? TargetFrameworkVersion = await configuration.TargetFrameworkVersion.GetDisplayValueAsync();

                if (string.Equals(TargetFrameworkIdentifier, s_netCoreTargetFrameworkIdentifier, StringComparisons.FrameworkIdentifiers) &&
                !string.IsNullOrEmpty(TargetFrameworkVersion) &&
                s_packageVersionToComponentId.TryGetValue(TargetFrameworkVersion, out var componentId))
                {
                    _missingSetupComponentRegistrationService.RegisterMissingSdkRuntimeComponentId(_projectGuid, project, componentId);
                }
            }
        }
    }
}
