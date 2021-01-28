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

        /// <summary>
        /// Logs a telemetry events from package restore components.
        /// </summary>
        /// <param name="packageRestoreTelemetryEvent">The details of which package restore operation is occuring.</param>
        public void LogPackageRestoreEvent(PackageRestoreTelemetryEvent packageRestoreTelemetryEvent)
        {
            string projectId = GetProjectId(packageRestoreTelemetryEvent.FullPath);

            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.PackageRestoreOperation, packageRestoreTelemetryEvent.PackageRestoreOperationName),
                    ( TelemetryPropertyName.PackageRestoreProjectId,  projectId),
                });
        }

        /// <summary>
        /// Logs a telemetry events from package restore components including whether package restore is up to date.
        /// </summary>
        /// <param name="packageRestoreUpToDateTelemetryEvent">The details of which package restore operation is occuring.</param>
        public void LogPackageRestoreEvent(PackageRestoreUpToDateTelemetryEvent packageRestoreUpToDateTelemetryEvent)
        {
            string projectId = GetProjectId(packageRestoreUpToDateTelemetryEvent.FullPath);

            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.PackageRestoreIsUpToDate, packageRestoreUpToDateTelemetryEvent.IsRestoreUpToDate),
                    ( TelemetryPropertyName.PackageRestoreOperation, packageRestoreUpToDateTelemetryEvent.PackageRestoreOperationName),
                    ( TelemetryPropertyName.PackageRestoreProjectId,  projectId),
                });
        }
    }
}
