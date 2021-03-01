// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IUnconfiguredProjectLanguageServiceTelemetryService))]
    internal class UnconfiguredProjectLanguageServiceTelemetryService : IUnconfiguredProjectLanguageServiceTelemetryService
    {
        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private readonly AsyncLazy<Guid> _projectGuidLazy;

        [ImportingConstructor]
        public UnconfiguredProjectLanguageServiceTelemetryService(UnconfiguredProject project, ITelemetryService telemetryService, IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _telemetryService = telemetryService;

            _projectGuidLazy = new AsyncLazy<Guid>(async () =>
            {
                return await _project.GetProjectGuidAsync();
            }, projectThreadingService.JoinableTaskFactory);
        }

        private Guid ProjectGuid => _projectGuidLazy.GetValue();

        public void PostActiveWorkspaceProjectContextHostPublishingEvent()
        {
            PostLanguageServiceEvent(LanguageServiceOperationNames.ActiveWorkspaceProjectContextHostPublishing);
        }

        public void PostWorkspaceProjectContextHostInitiatorInitializedEvent()
        {
            PostLanguageServiceEvent(LanguageServiceOperationNames.WorkspaceProjectContextHostInitiatorInitialized);
        }

        private void PostLanguageServiceEvent(string languageServiceOperationName)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.WorkspaceContextProjectId, ProjectGuid),
                    (TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                });
        }
    }
}
