// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal partial class SDKVersionTelemetryServiceComponent
    {
        private class SDKVersionTelemetryServiceInstance : AbstractProjectDynamicLoadInstance
        {
            private const string TelemetryEventName = "SDKVersion";
            private const string ProjectProperty = "Project";
            private const string NameProperty = "Name";
            private const string VersionProperty = "Version";

            private readonly IUnconfiguredProjectVsServices _projectVsServices;
            private readonly ISafeProjectGuidService _projectGuidSevice;
            private readonly ITelemetryService _telemetryService;

            [ImportingConstructor]
            public SDKVersionTelemetryServiceInstance(
                IUnconfiguredProjectVsServices projectVsServices,
                ISafeProjectGuidService projectGuidSevice,
                ITelemetryService telemetryService)
                : base(projectVsServices.ThreadingService.JoinableTaskContext)
            {
                _projectVsServices = projectVsServices;
                _projectGuidSevice = projectGuidSevice;
                _telemetryService = telemetryService;
            }

            protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                ConfigurationGeneral projectProperties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);

                var name = (string)await projectProperties.SDKIdentifier.GetValueAsync().ConfigureAwait(false);
                var version = (string)await projectProperties.SDKVersion.GetValueAsync().ConfigureAwait(false);
                string projectId = await GetProjectIdAsync().ConfigureAwait(false);

                if (name == null || version == null || projectId == null)
                    return;

                _telemetryService.PostProperties(
                    FormattableString.Invariant($"{TelemetryEventName}"),
                    new List<(string, object)>
                    {
                        (ProjectProperty, projectId),
                        (NameProperty, name),
                        (VersionProperty, version)
                    });
            }

            protected override Task DisposeCoreAsync(bool initialized) => Task.CompletedTask;

            private async Task<string> GetProjectIdAsync()
            {
                Guid projectGuid = await _projectGuidSevice.GetProjectGuidAsync().ConfigureAwait(false);
                return projectGuid == Guid.Empty ? null : projectGuid.ToString();
            }
        }
    }
}
