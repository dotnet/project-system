// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IConfiguredProjectPackageRestoreTelemetryService))]
    internal class ConfiguredProjectPackageRestoreTelemetryService : IConfiguredProjectPackageRestoreTelemetryService
    {
        private readonly ConfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private string? _projectHash;

        [ImportingConstructor]
        public ConfiguredProjectPackageRestoreTelemetryService(ConfiguredProject project, ITelemetryService telemetryService)
        {
            _project = project;
            _telemetryService = telemetryService;
        }

        private string ProjectTelemetryId
        {
            get
            {
                if (Strings.IsNullOrEmpty(_projectHash))
                {
                    string? fullPath = _project?.UnconfiguredProject?.FullPath;
                    _projectHash = Strings.IsNullOrEmpty(fullPath) ? string.Empty : _telemetryService.HashValue(fullPath);
                }

                return _projectHash;
            }
        }

        public void PostPackageRestoreEvent(string packageRestoreOperationName)
        {
            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.PackageRestoreOperation, packageRestoreOperationName),
                    ( TelemetryPropertyName.PackageRestoreProjectId,  ProjectTelemetryId),
                });
        }

        public void PostPackageRestoreEvent(string packageRestoreOperationName, bool isRestoreUpToDate)
        {
            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.PackageRestoreIsUpToDate, isRestoreUpToDate),
                    ( TelemetryPropertyName.PackageRestoreOperation, packageRestoreOperationName),
                    ( TelemetryPropertyName.PackageRestoreProjectId,  ProjectTelemetryId),
                });
        }
    }
}
