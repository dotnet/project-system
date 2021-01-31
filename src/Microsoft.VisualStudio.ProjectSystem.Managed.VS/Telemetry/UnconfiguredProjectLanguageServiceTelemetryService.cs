// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IUnconfiguredProjectLanguageServiceTelemetryService))]
    internal class UnconfiguredProjectLanguageServiceTelemetryService : IUnconfiguredProjectLanguageServiceTelemetryService
    {
        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private string? _projectHash;

        [ImportingConstructor]
        public UnconfiguredProjectLanguageServiceTelemetryService(UnconfiguredProject project, ITelemetryService telemetryService)
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
                    string? fullPath = _project?.FullPath;
                    _projectHash = Strings.IsNullOrEmpty(fullPath) ? string.Empty : _telemetryService.HashValue(fullPath);
                }

                return _projectHash;
            }
        }

        public void PostLanguageServiceEvent(string languageServiceOperationName)
        {
            _telemetryService.PostProperties(TelemetryEventName.LanguageServiceOperation, new (string propertyName, object propertyValue)[]
                {
                    ( TelemetryPropertyName.WorkspaceContextProjectId,  ProjectTelemetryId),
                    ( TelemetryPropertyName.LanguageServiceOperationName, languageServiceOperationName),
                });
        }
    }
}
