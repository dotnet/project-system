// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPackageRestoreTelemetryService
    {
        /// <summary>
        /// Logs a telemetry events from package restore components.
        /// </summary>
        /// <param name="packageRestoreTelemetryEvent">The details of which package restore operation is occuring.</param>
        void LogPackageRestoreEvent(PackageRestoreTelemetryEvent packageRestoreTelemetryEvent);

        /// <summary>
        /// Logs a telemetry events from package restore components including whether package restore is up to date.
        /// </summary>
        /// <param name="packageRestoreUpToDateTelemetryEvent">The details of which package restore operation is occuring.</param>
        void LogPackageRestoreEvent(PackageRestoreUpToDateTelemetryEvent packageRestoreUpToDateTelemetryEvent);
    }
}
