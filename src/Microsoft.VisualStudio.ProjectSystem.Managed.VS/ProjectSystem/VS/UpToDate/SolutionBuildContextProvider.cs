// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

/// <summary>
/// Monitors solution build events, and up-to-date check results. Maintains the current <see cref="SolutionBuildContext"/>
/// as well as statistics about each solution build operation for reporting to the user and via telemetry.
/// </summary>
[Export(typeof(ISolutionBuildContextProvider))]
[Export(typeof(ISolutionBuildEventListener))]
internal sealed class SolutionBuildContextProvider : ISolutionBuildContextProvider, ISolutionBuildEventListener
{
    private readonly Stopwatch _stopwatch = new();

    private readonly IProjectThreadingService _threadingService;
    private readonly IVsUIService<IVsOutputWindow> _outputWindow;
    private readonly IFileSystem _fileSystem;

    private int _projectCount;
    private int _configuredProjectCount;
    private int _upToDateProjectCount;
    private int _outOfDateProjectCount;
    private int _acceleratedProjectCount;
    private int _accelerationCandidateProjectCount;
    private int _accelerationFileCopyCount;
    private int _accelerationEnabledCount;
    private int _accelerationDisabledCount;
    private int _accelerationUnspecifiedCount;
    private int _filesCheckedCount;
    private long _waitTime;
    private long _checkTime;
    private LogLevel _logLevel;
    private bool _hasRebuild;

    [ImportingConstructor]
    public SolutionBuildContextProvider(
        IProjectThreadingService threadingService,
        IFileSystem fileSystem,
        IVsUIService<SVsOutputWindow, IVsOutputWindow> outputWindow)
    {
        _threadingService = threadingService;
        _fileSystem = fileSystem;
        _outputWindow = outputWindow;
    }

    public SolutionBuildContext? CurrentSolutionBuildContext { get; private set; }

    public void NotifySolutionBuildStarting()
    {
        _stopwatch.Restart();

        _projectCount = 0;
        _configuredProjectCount = 0;
        _upToDateProjectCount = 0;
        _outOfDateProjectCount = 0;
        _acceleratedProjectCount = 0;
        _accelerationCandidateProjectCount = 0;
        _accelerationFileCopyCount = 0;
        _accelerationEnabledCount = 0;
        _accelerationDisabledCount = 0;
        _accelerationUnspecifiedCount = 0;
        _filesCheckedCount = 0;
        _waitTime = 0;
        _checkTime = 0;
        _logLevel = 0;
        _hasRebuild = false;

        CurrentSolutionBuildContext = new(_fileSystem);
    }

    public void NotifyProjectChecked(
        bool upToDate,
        bool? buildAccelerationEnabled,
        BuildAccelerationResult result,
        int configurationCount,
        int copyCount,
        int fileCount,
        TimeSpan waitTime,
        TimeSpan checkTime,
        LogLevel logLevel)
    {
        // Aggregate data in a thread safe way, as up-to-date checks can overlap

        Interlocked.Add(ref _configuredProjectCount, configurationCount);
        Interlocked.Add(ref _accelerationFileCopyCount, copyCount);
        Interlocked.Add(ref _filesCheckedCount, fileCount);
        Interlocked.Add(ref _waitTime, waitTime.Ticks);
        Interlocked.Add(ref _checkTime, checkTime.Ticks);

        // All projects will have the same log level, so we don't need any kind of comparison here
        _logLevel = logLevel;

        if (result != BuildAccelerationResult.EnabledAccelerated)
        {
            Interlocked.Increment(ref upToDate ? ref _upToDateProjectCount : ref _outOfDateProjectCount);
        }

        if (buildAccelerationEnabled is null)
        {
            Interlocked.Increment(ref _accelerationUnspecifiedCount);
        }
        else if (buildAccelerationEnabled is true)
        {
            Interlocked.Increment(ref _accelerationEnabledCount);
        }
        else
        {
            Interlocked.Increment(ref _accelerationDisabledCount);
        }

        if (result == BuildAccelerationResult.DisabledCandidate)
        {
            Interlocked.Increment(ref _accelerationCandidateProjectCount);
        }
        else if (result == BuildAccelerationResult.EnabledAccelerated)
        {
            Interlocked.Increment(ref _acceleratedProjectCount);
        }
    }

    public void NotifyProjectBuildStarting(bool isRebuild)
    {
        if (isRebuild)
        {
            _hasRebuild = true;
        }

        Interlocked.Increment(ref _projectCount);
    }

    public void NotifySolutionBuildCompleted(bool cancelled)
    {
        CurrentSolutionBuildContext = null;

        _stopwatch.Stop();

        if (cancelled || _hasRebuild)
        {
            // Don't report telemetry for rebuilds, or if the build was cancelled
            return;
        }

        _threadingService.VerifyOnUIThread();

        if (_acceleratedProjectCount != 0)
        {
            LogMessage(string.Format(VSResources.BuildAccelerationSummary_2, _acceleratedProjectCount, _accelerationFileCopyCount));
        }

        var telemetryEvent = new TelemetryEvent(TelemetryEventName.SolutionBuildSummary);

        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.DurationMillis, _stopwatch.Elapsed.TotalMilliseconds);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.ProjectCount, _projectCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.ConfiguredProjectCount, _configuredProjectCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.UpToDateProjectCount, _upToDateProjectCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.OutOfDateProjectCount, _outOfDateProjectCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.AcceleratedProjectCount, _acceleratedProjectCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.AccelerationCandidateProjectCount, _accelerationCandidateProjectCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.AccelerationFileCopyCount, _accelerationFileCopyCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.AccelerationEnabledCount, _accelerationEnabledCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.AccelerationDisabledCount, _accelerationDisabledCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.AccelerationUnspecifiedCount, _accelerationUnspecifiedCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.FilesCheckedCount, _filesCheckedCount);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.WaitTimeMillis, new TimeSpan(_waitTime).TotalMilliseconds);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.CheckTimeMillis, new TimeSpan(_checkTime).TotalMilliseconds);
        telemetryEvent.Properties.Add(TelemetryPropertyName.SolutionBuildSummary.LogLevel, _logLevel);

        TelemetryService.DefaultSession.PostEvent(telemetryEvent);

        void LogMessage(string message)
        {
            if (_outputWindow.Value.GetPane(VSConstants.GUID_BuildOutputWindowPane, out IVsOutputWindowPane? outputPane) == HResult.OK && outputPane is not null)
            {
                outputPane.OutputStringNoPump(message);
            }
        }
    }
}
