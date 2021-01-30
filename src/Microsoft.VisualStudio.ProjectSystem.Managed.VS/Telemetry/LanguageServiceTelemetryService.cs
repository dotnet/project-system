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

        public string HashValue(string value)
        {
            return _telemetryService.HashValue(value);
        }

        public void PostDesignTimeBuildFailureEvent(string projectId)
        {
            _telemetryService.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, false),
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  projectId),
                });
        }

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

        public void PostLanguageServiceEvent(string languageServiceOperationName, string projectId)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  projectId),
                    ( TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                });
        }

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
