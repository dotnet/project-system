// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ILanguageServiceTelemetryService
    {
        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        string HashValue(string value);

        /// <summary>
        /// Post a telemetry event when a design time build fails.
        /// </summary>
        /// <param name="projectId">A unique identifier for the project.</param>
        void PostDesignTimeBuildFailureEvent(string projectId);

        /// <summary>
        /// Post a language service telemetry event without any corresponding metadata like project IDs.
        /// </summary>
        /// <param name="languageServiceTelemetryEvent">The language service telemetry event with details to be posted.</param>
        void PostLanguageServiceEvent(LanguageServiceTelemetryEvent languageServiceTelemetryEvent);

        /// <summary>
        /// Post a telemetry event with information about changes being applied to a project.
        /// </summary>
        /// <param name="applyProjectChangesTelemetryEvent">The telemetry information to post.</param>
        void PostLanguageServiceEvent(LanguageServiceApplyProjectChangesTelemetryEvent applyProjectChangesTelemetryEvent);

        /// <summary>
        /// Post a telemetry event when a event with an operation counter has been processed.
        /// </summary>
        /// <param name="languageServiceTelemetryEvent">The telemetry information to post.</param>
        void PostLanguageServiceEvent(LanguageServiceCountableOperationsTelemetryEvent languageServiceTelemetryEvent);
    }
}
