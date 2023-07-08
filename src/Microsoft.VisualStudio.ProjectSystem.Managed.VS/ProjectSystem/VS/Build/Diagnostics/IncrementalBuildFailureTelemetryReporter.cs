// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        private readonly IProjectSystemOptions _projectSystemOptions;
        private bool _hasBeenReported;

        [ImportingConstructor]
        public IncrementalBuildFailureTelemetryReporter(
            UnconfiguredProject _, // scoping
            IProjectSystemOptions projectSystemOptions)
        {
            _projectSystemOptions = projectSystemOptions;
        }

        public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken)
        {
            if (_hasBeenReported)
            {
                // Only report once per project. If we have previously reported this,
                // return false.
                return new ValueTask<bool>(false);
            }

            return _projectSystemOptions.IsIncrementalBuildFailureTelemetryEnabledAsync(cancellationToken);
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
