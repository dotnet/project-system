// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

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
        /// <param name="languageServiceOperationName">The language service telemetry event with details to be posted.</param>
        public void PostLanguageServiceEvent(string languageServiceOperationName)
        {
            _telemetryService.PostProperty(
                TelemetryEventName.LanguageServiceOperation,
                TelemetryPropertyName.LanguageServiceOperationName,
                languageServiceOperationName);
        }

        /// <summary>
        /// Post a telemetry event with information about changes being applied to a project.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the language service operation.</param>
        /// <param name="state">The state of the language service operation.</param>
        public void PostLanguageServiceEvent(string languageServiceOperationName, ContextState state)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    ( TelemetryPropertyName.WorkspaceContextIsActiveConfiguration, state.IsActiveConfiguration),
                    ( TelemetryPropertyName.WorkspaceContextIsActiveEditorContext, state.IsActiveEditorContext),
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  state.ProjectId),
                });
        }

        /// <summary>
        /// Post a language service telemetry event without any corresponding metadata like project IDs.
        /// </summary>
        /// <param name="languageServiceOperationName">The language service telemetry event with details to be posted.</param>
        /// <param name="projectId">A unique identifier for the project.</param>
        public void PostLanguageServiceEvent(string languageServiceOperationName, string projectId)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  projectId),
                    ( TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                });
        }

        /// <summary>
        /// Post a telemetry event when a event with an operation counter has been processed.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the language service operation.</param>
        /// <param name="projectId">A unique identifier for the project.</param>
        /// <param name="operationCount">The number of times the operation has been performed.</param>
        public void PostLanguageServiceEvent(string languageServiceOperationName, string projectId, int operationCount)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  projectId),
                    ( TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    ( TelemetryPropertyName.LanguageServiceOperationCount, operationCount),
                });
        }
    }
}
