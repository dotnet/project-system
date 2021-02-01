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
        private string? _projectHash;

        [ImportingConstructor]
        public ConfiguredProjectLanguageServiceTelemetryService(ConfiguredProject project, ITelemetryService telemetryService)
        {
            _project = project;
            _telemetryService = telemetryService;
        }

        private string ProjectTelemetryId
        {
            get
            {
                if (Strings.IsNullOrEmpty(_projectHash))
                {
                    string? fullPath = _project?.UnconfiguredProject?.FullPath;
                    _projectHash = Strings.IsNullOrEmpty(fullPath) ? string.Empty : _telemetryService.HashValue(fullPath);
                }

                return _projectHash;
            }
        }

        public void PostDesignTimeBuildFailureEvent()
        {
            _telemetryService.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, BoxedValues.False),
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                });
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName, ContextState state)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    (TelemetryPropertyName.WorkspaceContextIsActiveConfiguration, state.IsActiveConfiguration),
                    (TelemetryPropertyName.WorkspaceContextIsActiveEditorContext, state.IsActiveEditorContext),
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                });
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectTelemetryId),
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
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
