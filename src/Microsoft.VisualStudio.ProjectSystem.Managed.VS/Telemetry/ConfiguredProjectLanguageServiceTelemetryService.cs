// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Managed;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IConfiguredProjectLanguageServiceTelemetryService))]
    internal class ConfiguredProjectLanguageServiceTelemetryService : IConfiguredProjectLanguageServiceTelemetryService
    {
        private readonly ConfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private readonly AsyncLazy<Guid> _projectGuidLazy;

        [ImportingConstructor]
        public ConfiguredProjectLanguageServiceTelemetryService(ConfiguredProject project, ITelemetryService telemetryService, IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _telemetryService = telemetryService;

            _projectGuidLazy = new AsyncLazy<Guid>(async () =>
            {
                return await _project.UnconfiguredProject.GetProjectGuidAsync();
            }, projectThreadingService.JoinableTaskFactory);
        }

        private Guid ProjectGuid => _projectGuidLazy.GetValue();

        public void PostDesignTimeBuildFailureEvent()
        {
            _telemetryService.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, BoxedValues.False),
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectGuid),
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
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectGuid),
                    (TelemetryPropertyName.WorkspaceContextId, workspaceContextId),
                    (TelemetryPropertyName.WorkspaceContextEventId, eventId),
                });
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName, long workspaceContextId)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectGuid),
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    (TelemetryPropertyName.WorkspaceContextId, workspaceContextId),
                });
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName, int operationCount)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectGuid),
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                    (TelemetryPropertyName.LanguageServiceOperationCount, operationCount),
                });
        }
    }
}
