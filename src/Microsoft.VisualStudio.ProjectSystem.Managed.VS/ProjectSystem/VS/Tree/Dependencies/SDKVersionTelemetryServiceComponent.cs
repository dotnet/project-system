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
    internal partial class SDKVersionTelemetryServiceComponent : AbstractProjectDynamicLoadComponent
    {
        private readonly ProjectProperties _projectProperties;
        private readonly ITelemetryService _telemetryService;
        private readonly IProjectThreadingService _projectThreadingService;
        private readonly ISafeProjectGuidService _projectGuidSevice;

        [ImportingConstructor]
        public SDKVersionTelemetryServiceComponent(
            ProjectProperties projectProperties,
            ISafeProjectGuidService projectGuidSevice,
            ITelemetryService telemetryService,
            IProjectThreadingService projectThreadingService)
            : base(projectThreadingService.JoinableTaskContext)
        {
            _projectProperties = projectProperties;
            _projectGuidSevice = projectGuidSevice;
            _telemetryService = telemetryService;
            _projectThreadingService = projectThreadingService;
        }

        protected override AbstractProjectDynamicLoadInstance CreateInstance()
            => new SDKVersionTelemetryServiceInstance(_projectProperties, _projectGuidSevice, _telemetryService, _projectThreadingService);
    }
}
