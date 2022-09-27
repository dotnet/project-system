// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    /// <summary>
    ///   Reports incremental build failures via telemetry.
    /// </summary>
    [Export(typeof(IIncrementalBuildFailureReporter))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal sealed class IncrementalBuildFailureTelemetryReporter : IIncrementalBuildFailureReporter
    {
        private readonly UnconfiguredProject _project;
        private readonly IVsUIService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;
        private bool _hasBeenReported;

        [ImportingConstructor]
        public IncrementalBuildFailureTelemetryReporter(
            UnconfiguredProject project,
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService)
        {
            _project = project;
            _featureFlagsService = featureFlagsService;
        }

        public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
        {
            if (_hasBeenReported)
            {
                // Only report once per project. If we have previously reported this,
                // return false.
                return false;
            }

            await _project.Services.ThreadingPolicy.SwitchToUIThread(cancellationToken);

            IVsFeatureFlags featureFlagsService = _featureFlagsService.Value;

            return featureFlagsService.IsFeatureEnabled(FeatureFlagNames.EnableIncrementalBuildFailureTelemetry, defaultValue: false);
        }

        public Task ReportFailureAsync(string failureReason, string failureDescription, TimeSpan checkDuration, CancellationToken cancellationToken)
        {
            Assumes.False(_hasBeenReported);

            var telemetryEvent = new TelemetryEvent(TelemetryEventName.IncrementalBuildValidationFailure);

            telemetryEvent.Properties.Add(TelemetryPropertyName.IncrementalBuildValidation.FailureReason, failureReason);
            telemetryEvent.Properties.Add(TelemetryPropertyName.IncrementalBuildValidation.DurationMillis, checkDuration);

            TelemetryService.DefaultSession.PostEvent(telemetryEvent);

            _hasBeenReported = true;

            return Task.CompletedTask;
        }
    }
}
