// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IConfiguredProjectPackageRestoreTelemetryService))]
    internal class ConfiguredProjectPackageRestoreTelemetryService : IConfiguredProjectPackageRestoreTelemetryService
    {
        private readonly ConfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private readonly AsyncLazy<Guid> _projectGuidLazy;

        [ImportingConstructor]
        public ConfiguredProjectPackageRestoreTelemetryService(ConfiguredProject project, ITelemetryService telemetryService, IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _telemetryService = telemetryService;

            _projectGuidLazy = new AsyncLazy<Guid>(async () =>
            {
                return await _project.UnconfiguredProject.GetProjectGuidAsync();
            }, projectThreadingService.JoinableTaskFactory);
        }

        private Guid ProjectGuid => _projectGuidLazy.GetValue();

        public void PostPackageRestoreEvent(string packageRestoreOperationName)
        {
            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.PackageRestoreOperation, packageRestoreOperationName),
                    (TelemetryPropertyName.PackageRestoreProjectId, ProjectGuid),
                });
        }

        public void PostPackageRestoreEvent(string packageRestoreOperationName, long packageRestoreProgressTrackerId)
        {
            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.PackageRestoreOperation, packageRestoreOperationName),
                    (TelemetryPropertyName.PackageRestoreProjectId, ProjectGuid),
                    (TelemetryPropertyName.PackageRestoreProgressTrackerId, packageRestoreProgressTrackerId),
                });
        }

        public void PostPackageRestoreCompletedEvent(bool isRestoreUpToDate, long packageRestoreProgressTrackerId)
        {
            _telemetryService.PostProperties(TelemetryEventName.ProcessPackageRestore, new (string propertyName, object propertyValue)[]
                {
                    (TelemetryPropertyName.PackageRestoreIsUpToDate, isRestoreUpToDate),
                    (TelemetryPropertyName.PackageRestoreOperation, PackageRestoreOperationNames.PackageRestoreProgressTrackerRestoreCompleted),
                    (TelemetryPropertyName.PackageRestoreProjectId, ProjectGuid),
                    (TelemetryPropertyName.PackageRestoreProgressTrackerId, packageRestoreProgressTrackerId),
                });
        }
    }
}
