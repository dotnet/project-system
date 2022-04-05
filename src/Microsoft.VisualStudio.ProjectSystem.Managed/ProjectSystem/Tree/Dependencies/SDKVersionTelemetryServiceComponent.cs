// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// For maintaining light state about the SDK version used in a project
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal partial class SDKVersionTelemetryServiceComponent : AbstractMultiLifetimeComponent<SDKVersionTelemetryServiceComponent.SDKVersionTelemetryServiceInstance>, IProjectDynamicLoadComponent
    {
        private readonly IUnconfiguredProjectCommonServices _projectVsServices;
        private readonly ITelemetryService _telemetryService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

        [ImportingConstructor]
        public SDKVersionTelemetryServiceComponent(
            IUnconfiguredProjectCommonServices projectVsServices,
            ISafeProjectGuidService projectGuidService,
            ITelemetryService telemetryService,
            IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _projectGuidService = projectGuidService;
            _telemetryService = telemetryService;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
        }

        protected override SDKVersionTelemetryServiceInstance CreateInstance()
            => new(
                _projectVsServices,
                _projectGuidService,
                _telemetryService,
                _unconfiguredProjectTasksService);
    }
}
