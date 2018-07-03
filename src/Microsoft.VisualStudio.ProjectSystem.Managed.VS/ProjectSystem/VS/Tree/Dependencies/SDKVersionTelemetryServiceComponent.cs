// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// For maintaining light state about the SDK version used in a project
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal partial class SDKVersionTelemetryServiceComponent : AbstractProjectDynamicLoadComponent
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly ITelemetryService _telemetryService;
        private readonly ISafeProjectGuidService _projectGuidSevice;

        [ImportingConstructor]
        public SDKVersionTelemetryServiceComponent(
            IUnconfiguredProjectVsServices projectVsServices,
            ISafeProjectGuidService projectGuidSevice,
            ITelemetryService telemetryService)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _projectGuidSevice = projectGuidSevice;
            _telemetryService = telemetryService;
        }

        protected override AbstractProjectDynamicLoadInstance CreateInstance()
            => new SDKVersionTelemetryServiceInstance(_projectVsServices, _projectGuidSevice, _telemetryService);
    }
}
