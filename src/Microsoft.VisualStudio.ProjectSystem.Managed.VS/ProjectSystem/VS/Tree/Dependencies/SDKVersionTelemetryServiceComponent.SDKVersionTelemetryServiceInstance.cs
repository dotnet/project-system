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
            private readonly ITelemetryService _telemetryService;
            private string _projectId;

            [ImportingConstructor]
            public SDKVersionTelemetryServiceInstance(
                IUnconfiguredProjectVsServices projectVsServices,
                ITelemetryService telemetryService)
                : base(projectVsServices.ThreadingService.JoinableTaskContext)
            {
                _projectVsServices = projectVsServices;
                _telemetryService = telemetryService;
            }

            protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                await _projectVsServices.ThreadingService.SwitchToUIThread();
                ConfigurationGeneral projectProperties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);

                var name = (string)await projectProperties.SDKIdentifier.GetValueAsync().ConfigureAwait(false);
                var version = (string)await projectProperties.SDKVersion.GetValueAsync().ConfigureAwait(false);

                if (_projectId == null)
                {
                    InitializeProjectId();
                }

                _telemetryService.PostProperties(
                    FormattableString.Invariant($"{TelemetryEventName}"),
                    new List<(string, object)>
                    {
                        (ProjectProperty, _projectId),
                        (NameProperty, name),
                        (VersionProperty, version)
                    });
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                _projectId = null;
                return Task.CompletedTask;
            }

            private void InitializeProjectId()
            {
                IProjectGuidService projectGuidService = _projectVsServices.Project.Services.ExportProvider.GetExportedValueOrDefault<IProjectGuidService>();
                if (projectGuidService != null)
                {
                    _projectId = projectGuidService.ProjectGuid.ToString();
                }
                else
                {
                    _projectId = _telemetryService.HashValue(_projectVsServices.Project.FullPath);
                }
            }
        }
    }
}
