// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    /// <summary>
    ///   Reports incremental build failures via the output window.
    /// </summary>
    [Export(typeof(IIncrementalBuildFailureReporter))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal sealed class IncrementalBuildFailureOutputWindowReporter : IIncrementalBuildFailureReporter
    {
        private readonly UnconfiguredProject _project;
        private readonly IVsUIService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;
        private readonly IVsUIService<SVsOutputWindow, IVsOutputWindow> _outputWindow;
        private readonly IProjectSystemOptions _projectSystemOptions;

        [ImportingConstructor]
        public IncrementalBuildFailureOutputWindowReporter(
            UnconfiguredProject project,
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            IVsUIService<SVsOutputWindow, IVsOutputWindow> outputWindow,
            IProjectSystemOptions projectSystemOptions)
        {
            _project = project;
            _featureFlagsService = featureFlagsService;
            _outputWindow = outputWindow;
            _projectSystemOptions = projectSystemOptions;
        }

        public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
        {
            LogLevel logLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(cancellationToken);

            if (logLevel != LogLevel.None)
            {
                // Always display if the user has enabled any kind of up-to-date check logging
                return true;
            }

            await _project.Services.ThreadingPolicy.SwitchToUIThread(cancellationToken);

            IVsFeatureFlags featureFlagsService = _featureFlagsService.Value;

            return featureFlagsService.IsFeatureEnabled(FeatureFlagNames.EnableIncrementalBuildFailureOutputLogging, defaultValue: false);
        }

        public async Task ReportFailureAsync(string failureReason, string failureDescription, TimeSpan checkDuration, CancellationToken cancellationToken)
        {
            // Report telemetry indicating that we showed this message. This will allow us to compare the number of times this is shown
            // with the click-through rate for more information.
            var telemetryEvent = new TelemetryEvent(TelemetryEventName.IncrementalBuildValidationFailureDisplayed);

            telemetryEvent.Properties.Add(TelemetryPropertyName.IncrementalBuildValidation.FailureReason, failureReason);
            telemetryEvent.Properties.Add(TelemetryPropertyName.IncrementalBuildValidation.DurationMillis, checkDuration);

            TelemetryService.DefaultSession.PostEvent(telemetryEvent);

            await _project.Services.ThreadingPolicy.SwitchToUIThread(cancellationToken);

            // Log a message to the output window. This message will appear after the build completes, which is fine.
            // We don't want to hold up reporting of a completed build on this.
            Guid outputPaneGuid = VSConstants.GUID_BuildOutputWindowPane;

            if (_outputWindow.Value.GetPane(ref outputPaneGuid, out IVsOutputWindowPane? outputPane) == HResult.OK && outputPane is not null)
            {
                string message = string.Format(VSResources.IncrementalBuildFailureWarningMessage_2, Path.GetFileName(_project.FullPath), failureDescription.TrimEnd(Delimiter.Period));

                outputPane.OutputStringNoPump(message);
            }
        }
    }
}
