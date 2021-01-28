// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ILanguageServiceTelemetryService))]
    internal class LanguageServiceTelemetryService : ILanguageServiceTelemetryService
    {
        private readonly ITelemetryService _telemetryService;

        [ImportingConstructor]
        public LanguageServiceTelemetryService(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        public string HashValue(string value)
        {
            return _telemetryService.HashValue(value);
        }

        /// <summary>
        /// Post a telemetry event when a design time build fails.
        /// </summary>
        /// <param name="projectId">A unique identifier for the project.</param>
        public void PostDesignTimeBuildFailureEvent(string projectId)
        {
            _telemetryService.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, false),
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  projectId),
                });
        }

        /// <summary>
        /// Post a language service telemetry event without any corresponding metadata like project IDs.
        /// </summary>
        /// <param name="languageServiceTelemetryEvent">The language service telemetry event with details to be posted.</param>
        public void PostLanguageServiceEvent(LanguageServiceTelemetryEvent languageServiceTelemetryEvent)
        {
            _telemetryService.PostProperty(
                TelemetryEventName.LanguageServiceOperation,
                TelemetryPropertyName.LanguageServiceOperationName,
                languageServiceTelemetryEvent.LanguageServiceOperationName);
        }

        /// <summary>
        /// Post a telemetry event with information about changes being applied to a project.
        /// </summary>
        /// <param name="applyProjectChangesTelemetryEvent">The telemetry information to post.</param>
        public void PostLanguageServiceEvent(LanguageServiceApplyProjectChangesTelemetryEvent applyProjectChangesTelemetryEvent)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.LanguageServiceOperationName, applyProjectChangesTelemetryEvent.LanguageServiceOperationName),
                    ( TelemetryPropertyName.WorkspaceContextIsActiveConfiguration, applyProjectChangesTelemetryEvent.State.IsActiveConfiguration),
                    ( TelemetryPropertyName.WorkspaceContextIsActiveEditorContext, applyProjectChangesTelemetryEvent.State.IsActiveEditorContext),
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  applyProjectChangesTelemetryEvent.State.ProjectId),
                });
        }

        /// <summary>
        /// Post a telemetry event when a event with an operation counter has been processed.
        /// </summary>
        /// <param name="languageServiceTelemetryEvent">The telemetry information to post.</param>
        public void PostLanguageServiceEvent(LanguageServiceCountableOperationsTelemetryEvent languageServiceTelemetryEvent)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  languageServiceTelemetryEvent.ProjectId),
                    ( TelemetryPropertyName.LanguageServiceOperationName, languageServiceTelemetryEvent.LanguageServiceOperationName),
                    ( TelemetryPropertyName.LanguageServiceOperationCount, languageServiceTelemetryEvent.OperationCount),
                });
        }
    }
}
