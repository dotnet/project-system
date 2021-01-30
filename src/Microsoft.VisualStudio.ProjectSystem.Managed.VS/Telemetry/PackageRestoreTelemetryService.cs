// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IPackageRestoreTelemetryService))]
    internal class PackageRestoreTelemetryService : IPackageRestoreTelemetryService
    {
        private readonly ITelemetryService _telemetryService;

        [ImportingConstructor]
        public PackageRestoreTelemetryService(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        private string GetProjectId(string fullPath) => string.IsNullOrEmpty(fullPath) ? string.Empty : _telemetryService.HashValue(fullPath);

        public void PostPackageRestoreEvent(string packageRestoreOperationName, string fullPath)
        {
            string projectId = GetProjectId(fullPath);

            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.PackageRestoreOperation, packageRestoreOperationName),
                    ( TelemetryPropertyName.PackageRestoreProjectId,  projectId),
                });
        }

        public void PostPackageRestoreEvent(string packageRestoreOperationName, string fullPath, bool isRestoreUpToDate)
        {
            string projectId = GetProjectId(fullPath);

            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.PackageRestoreIsUpToDate, isRestoreUpToDate),
                    ( TelemetryPropertyName.PackageRestoreOperation, packageRestoreOperationName),
                    ( TelemetryPropertyName.PackageRestoreProjectId,  projectId),
                });
        }
    }
}
