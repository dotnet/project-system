// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Managed;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IConfiguredProjectLanguageServiceTelemetryService))]
    internal class ConfiguredProjectLanguageServiceTelemetryService : IConfiguredProjectLanguageServiceTelemetryService
    {
        private readonly ConfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private string? _projectTelemetryId;

        [ImportingConstructor]
        public ConfiguredProjectLanguageServiceTelemetryService(ConfiguredProject project, ITelemetryService telemetryService)
        {
            _project = project;
            _telemetryService = telemetryService;
        }

        private string ProjectTelemetryId => _projectTelemetryId ??= _telemetryService.GetProjectId(_project.UnconfiguredProject);

        public void PostDesignTimeBuildFailureEvent()
        {
            _telemetryService.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, BoxedValues.False),
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                });
        }

        public void PostApplyProjectChangesEvent(ContextState state, long workspaceContextId, long eventId, bool starting)
        {
            string languageServiceOperationName = starting ? LanguageServiceOperationNames.ApplyingProjectChangesStarted : LanguageServiceOperationNames.ApplyingProjectChangesCompleted;

            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    (TelemetryPropertyName.WorkspaceContextIsActiveConfiguration, state.IsActiveConfiguration),
                    (TelemetryPropertyName.WorkspaceContextIsActiveEditorContext, state.IsActiveEditorContext),
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                    (TelemetryPropertyName.WorkspaceContextId, workspaceContextId),
                    (TelemetryPropertyName.WorkspaceContextEventId, eventId),
                });
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName, long workspaceContextId)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    (TelemetryPropertyName.WorkspaceContextId, workspaceContextId),
                });
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName, int operationCount)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    (TelemetryPropertyName.LanguageServiceOperationCount, operationCount),
                });
        }
    }
}
