// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly ITelemetryService _telemetryService;
        private readonly ISafeProjectGuidService _projectGuidSevice;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

        [ImportingConstructor]
        public SDKVersionTelemetryServiceComponent(
            IUnconfiguredProjectVsServices projectVsServices,
            ISafeProjectGuidService projectGuidSevice,
            ITelemetryService telemetryService,
            IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _projectGuidSevice = projectGuidSevice;
            _telemetryService = telemetryService;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
        }

        protected override AbstractProjectDynamicLoadInstance CreateInstance()
            => new SDKVersionTelemetryServiceInstance(
                _projectVsServices,
                _projectGuidSevice,
                _telemetryService,
                _unconfiguredProjectTasksService,
                args => OnNoSDKDetected?.Invoke(this, args));

        // For testing only
        internal event EventHandler<NoSDKDetectedEventArgs> OnNoSDKDetected;

        internal class NoSDKDetectedEventArgs : EventArgs
        {
            public NoSDKDetectedEventArgs(string projectGuid, string version)
            {
                ProjectGuid = projectGuid;
                Version = version;
            }

            public string ProjectGuid { get; }
            public string Version { get; }
        }
    }
}
