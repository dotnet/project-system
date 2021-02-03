// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IConfiguredProjectPackageRestoreTelemetryService
    {
        /// <summary>
        /// Posts a telemetry event from a package restore component.
        /// </summary>
        /// <param name="packageRestoreOperationName">The name of the specific package restore operation.</param>
        void PostPackageRestoreEvent(string packageRestoreOperationName);

        /// <summary>
        /// Posts a telemetry event from a package restore component.
        /// </summary>
        /// <param name="packageRestoreOperationName">The name of the specific package restore operation.</param>
        /// <param name="packageRestoreProgressTrackerId">An identifier to enable correlation of events.</param>
        void PostPackageRestoreEvent(string packageRestoreOperationName, long packageRestoreProgressTrackerId);

        /// <summary>
        /// Posts a telemetry event from package restore components including whether package restore is up to date.
        /// </summary>
        /// <param name="packageRestoreOperationName">The name of the specific package restore operation.</param>
        /// <param name="isRestoreUpToDate">Flag indicating whether the restore is up to date.</param>
        /// <param name="packageRestoreProgressTrackerId">An identifier to enable correlation of events.</param>
        void PostPackageRestoreEvent(string packageRestoreOperationName, bool isRestoreUpToDate, long packageRestoreProgressTrackerId);
    }
}
