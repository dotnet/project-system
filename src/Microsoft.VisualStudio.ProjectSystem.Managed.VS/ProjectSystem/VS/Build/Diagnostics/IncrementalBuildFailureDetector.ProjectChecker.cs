// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    internal sealed partial class IncrementalBuildFailureDetector
    {
        [Export(typeof(IProjectChecker))]
        internal sealed class ProjectChecker : IProjectChecker
        {
            private readonly UnconfiguredProject _project;
            private readonly IActiveConfiguredValue<IBuildUpToDateCheckProvider> _upToDateCheckProvider;
            private readonly IActiveConfiguredValue<IBuildUpToDateCheckValidator> _upToDateCheckValidator;
            private readonly IVsUIService<SVsOutputWindow, IVsOutputWindow> _outputWindow;

            private bool _hasBeenReportedViaTelemetry;

            [ImportingConstructor]
            public ProjectChecker(
                UnconfiguredProject project,
                IActiveConfiguredValue<IBuildUpToDateCheckProvider> upToDateCheckProvider,
                IActiveConfiguredValue<IBuildUpToDateCheckValidator> upToDateCheckValidator,
                IVsUIService<SVsOutputWindow, IVsOutputWindow> outputWindow)
            {
                _project = project;
                _upToDateCheckProvider = upToDateCheckProvider;
                _upToDateCheckValidator = upToDateCheckValidator;
                _outputWindow = outputWindow;
            }

            public async Task CheckAsync(
                BuildAction buildAction,
                bool telemetryEnabled,
                bool outputLoggingEnabled,
                CancellationToken cancellationToken)
            {
                Assumes.True(telemetryEnabled || outputLoggingEnabled);

                if (!outputLoggingEnabled && _hasBeenReportedViaTelemetry)
                {
                    return;
                }

                var sw = Stopwatch.StartNew();

                if (!await _upToDateCheckProvider.Value.IsUpToDateCheckEnabledAsync(cancellationToken))
                {
                    // The fast up-to-date check has been disabled. We can't know the reason why.
                    // We currently do not flag errors in this case, so stop processing immediately.
                    return;
                }

                (bool isUpToDate, string? failureReason) = await _upToDateCheckValidator.Value.ValidateUpToDateAsync(buildAction, cancellationToken);

                if (isUpToDate)
                {
                    // The project is up-to-date, as expected. Nothing more to do.
                    return;
                }

                if (telemetryEnabled && !_hasBeenReportedViaTelemetry)
                {
                    // We currently report once per project per solution lifetime.

                    var telemetryEvent = new TelemetryEvent(TelemetryEventName.IncrementalBuildValidationFailure);
                    telemetryEvent.Properties.Add(TelemetryPropertyName.IncrementalBuildFailureReason, failureReason);
                    telemetryEvent.Properties.Add(TelemetryPropertyName.IncrementalBuildValidationDurationMillis, sw.Elapsed.TotalMilliseconds);
                    TelemetryService.DefaultSession.PostEvent(telemetryEvent);
                    _hasBeenReportedViaTelemetry = true;
                }

                if (outputLoggingEnabled)
                {
                    await LogWarningInBuildOutputAsync();
                }

                return;

                async Task LogWarningInBuildOutputAsync()
                {
                    await _project.Services.ThreadingPolicy.SwitchToUIThread(cancellationToken);

                    Guid outputPaneGuid = VSConstants.GUID_BuildOutputWindowPane;

                    if (_outputWindow.Value.GetPane(ref outputPaneGuid, out IVsOutputWindowPane? outputPane) == HResult.OK && outputPane != null)
                    {
                        string message = string.Format(VSResources.IncrementalBuildFailureWarningMessage, System.IO.Path.GetFileName(_project.FullPath));

                        Marshal.ThrowExceptionForHR(outputPane.OutputStringThreadSafe(message));
                    }
                }
            }
        }
    }
}
