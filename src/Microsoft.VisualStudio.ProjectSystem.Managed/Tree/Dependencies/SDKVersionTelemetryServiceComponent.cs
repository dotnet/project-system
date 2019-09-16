// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
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
            => new SDKVersionTelemetryServiceInstance(
                _projectVsServices,
                _projectGuidService,
                _telemetryService,
                _unconfiguredProjectTasksService);
    }
}
