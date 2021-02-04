// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    /**
     * The process of requesting NuGet restore for projects is done by invoking the NuGet project
     * nomination APIs. This is necessary to ensure that all the information necessary for features
     * like intellisense is generated. Once nomination has completed and the package restore is up
     * to date, the package restore process needs to notify the operation progress system that package
     * restore has completed. This interface provides methods to enable this notification by posting
     * this event:
     * 
     *  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     * | Event Name                                             | Property Name                                                | Property Value                                                                           |
     * |------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
     * | <see cref="TelemetryEventName.ProcessPackageRestore"/> | <see cref="TelemetryPropertyName.PackageRestoreOperation"/>  | <see cref="PackageRestoreOperationNames.PackageRestoreProgressTrackerRestoreCompleted"/> |
     * |                                                        | <see cref="TelemetryPropertyName.PackageRestoreIsUpToDate"/> | True when operation progress is notified                                                 |
     *  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     *
     * It also enables the package restore components to post telemetry events indicating
     * whether the initialization of the package restore components (for the ConfiguredProject) suceeded.
     */
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
        /// Posts a <see cref="TelemetryEventName.ProcessPackageRestore"/> event with a
        /// <see cref="TelemetryPropertyName.PackageRestoreIsUpToDate"/> property whose value will be set to
        /// indicate whether package restore is up to date.
        /// </summary>
        /// <param name="isRestoreUpToDate">Flag indicating whether the restore is up to date.</param>
        /// <param name="packageRestoreProgressTrackerId">An identifier to enable correlation of events.</param>
        void PostPackageRestoreCompletedEvent(bool isRestoreUpToDate, long packageRestoreProgressTrackerId);
    }
}
