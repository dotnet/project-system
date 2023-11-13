// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        internal sealed class Log
        {
            private readonly TextWriter _writer;
            private readonly IBuildUpToDateCheckProvider _check;
            private readonly ICollection<Lazy<IBuildUpToDateCheckProvider, IOrderPrecedenceMetadataView>> _checkers;
            private readonly ISolutionBuildEventListener _solutionBuildEventListener;
            private readonly Stopwatch _stopwatch;
            private readonly TimeSpan _waitTime;
            private readonly TimestampCache _timestampCache;
            private readonly Guid _projectGuid;
            private readonly string _fileName;
            private readonly ITelemetryService? _telemetryService; // null for validation runs
            private readonly UpToDateCheckConfiguredInput _upToDateCheckConfiguredInput;
            private readonly string? _ignoreKinds;
            private readonly int _checkNumber;

            public LogLevel Level { get; }

            public int Indent { get; set; }

            public string? FailureReason { get; private set; }
            public string? FailureDescription { get; private set; }
            public FileSystemOperationAggregator? FileSystemOperations { get; set; }

            public Log(TextWriter writer, IBuildUpToDateCheckProvider check, ICollection<Lazy<IBuildUpToDateCheckProvider, IOrderPrecedenceMetadataView>> checkers, LogLevel requestedLogLevel, ISolutionBuildEventListener solutionBuildEventListener, Stopwatch stopwatch, TimeSpan waitTime, TimestampCache timestampCache, string projectPath, Guid projectGuid, ITelemetryService? telemetryService, UpToDateCheckConfiguredInput upToDateCheckConfiguredInput, string? ignoreKinds, int checkNumber)
            {
                _writer = writer;
                _check = check;
                _checkers = checkers;
                Level = requestedLogLevel;
                _solutionBuildEventListener = solutionBuildEventListener;
                _stopwatch = stopwatch;
                _waitTime = waitTime;
                _timestampCache = timestampCache;
                _projectGuid = projectGuid;
                _telemetryService = telemetryService;
                _upToDateCheckConfiguredInput = upToDateCheckConfiguredInput;
                _ignoreKinds = ignoreKinds;
                _checkNumber = checkNumber;

                _fileName = Path.GetFileNameWithoutExtension(projectPath);
            }

            public Scope IndentScope() => new(this);

            private string Preamble()
            {
                return Indent switch
                {
                    <= 0 => "FastUpToDate: ",
                    1 => "FastUpToDate:     ",
                    2 => "FastUpToDate:         ",
                    3 => "FastUpToDate:             ",
                    _ => "FastUpToDate: " + new string(' ', Indent * 4)
                };
            }

            private static string GetResourceString(string resourceName)
            {
                string? message = VSResources.ResourceManager.GetString(resourceName, VSResources.Culture);

                if (message is null)
                {
                    Assumes.Fail($"Resource with name '{resourceName}' not found.");
                }

                return message;
            }

            private void Write(LogLevel level, string resourceName, object arg0)
            {
                if (level <= Level)
                {
                    // These are user visible, so we want them in local times so that
                    // they correspond with dates/times that Explorer, etc shows
                    ConvertToLocalTime(ref arg0);

                    string message = GetResourceString(resourceName);

                    _writer.WriteLine($"{Preamble()}{string.Format(message, arg0)} ({_fileName})");
                }
            }

            private void Write(LogLevel level, string resourceName, object arg0, object arg1)
            {
                if (level <= Level)
                {
                    // These are user visible, so we want them in local times so that
                    // they correspond with dates/times that Explorer, etc shows
                    ConvertToLocalTime(ref arg0);
                    ConvertToLocalTime(ref arg1);

                    string message = GetResourceString(resourceName);

                    _writer.WriteLine($"{Preamble()}{string.Format(message, arg0, arg1)} ({_fileName})");
                }
            }

            private void Write(LogLevel level, string resourceName, params object[] values)
            {
                if (level <= Level)
                {
                    // These are user visible, so we want them in local times so that
                    // they correspond with dates/times that Explorer, etc shows
                    ConvertToLocalTimes(values);

                    string message = GetResourceString(resourceName);

                    _writer.WriteLine($"{Preamble()}{string.Format(message, values)} ({_fileName})");
                }
            }

            private void Write(LogLevel level, string resourceName)
            {
                if (level <= Level)
                {
                    string message = GetResourceString(resourceName);

                    _writer.WriteLine($"{Preamble()}{message} ({_fileName})");
                }
            }

            private void WriteLiteral(LogLevel level, string message)
            {
                if (level <= Level)
                {
                    _writer.WriteLine($"{Preamble()}{message} ({_fileName})");
                }
            }

            private static void ConvertToLocalTimes(object[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ConvertToLocalTime(ref values[i]);
                }
            }

            private static void ConvertToLocalTime(ref object value)
            {
                if (value is DateTime time)
                {
                    value = time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public void Minimal(string resourceName) => Write(LogLevel.Minimal, resourceName);
            public void Minimal(string resourceName, params object[] values) => Write(LogLevel.Minimal, resourceName, values);

            public void Info(string resourceName) => Write(LogLevel.Info, resourceName);
            public void Info(string resourceName, object arg0) => Write(LogLevel.Info, resourceName, arg0);
            public void Info(string resourceName, object arg0, object arg1) => Write(LogLevel.Info, resourceName, arg0, arg1);
            public void Info(string resourceName, params object[] values) => Write(LogLevel.Info, resourceName, values);

            public void Verbose(string resourceName) => Write(LogLevel.Verbose, resourceName);
            public void Verbose(string resourceName, object arg0) => Write(LogLevel.Verbose, resourceName, arg0);
            public void Verbose(string resourceName, params object[] values) => Write(LogLevel.Verbose, resourceName, values);
            public void VerboseLiteral(string message) => WriteLiteral(LogLevel.Verbose, message);

            /// <summary>
            /// Publishes the up-to-date failure via telemetry and the output window.
            /// </summary>
            /// <param name="reason">A string that uniquely identifies the kind of failure. Must not contain any PII such as file system data.</param>
            /// <param name="resourceName">The name of a resource string that clearly describes the reason for the failure. That string may contain PII, as this string is only displayed on screen.</param>
            /// <param name="values">Optional format arguments to be applied to the string resource with name <paramref name="resourceName"/>.</param>
            /// <returns><see langword="false"/>, which may be returned directly in <see cref="BuildUpToDateCheck"/>.</returns>
            public bool Fail(string reason, string resourceName, params object[] values)
            {
                Assumes.NotNull(FileSystemOperations);

                _stopwatch.Stop();

                // We may be indented when a failure is identified. Set the indent to zero so
                // we always flush-align the failure message.
                Indent = 0;

                // Minimal logging only includes failures.
                Minimal(resourceName, values);

                _solutionBuildEventListener.NotifyProjectChecked(
                    upToDate: false,
                    buildAccelerationEnabled: FileSystemOperations.IsAccelerationEnabled,
                    result: FileSystemOperations.AccelerationResult,
                    configurationCount: _upToDateCheckConfiguredInput.ImplicitInputs.Length,
                    copyCount: 0,
                    fileCount: _timestampCache.Count,
                    waitTime: _waitTime,
                    checkTime: _stopwatch.Elapsed,
                    logLevel: Level);

                // Send telemetry.
                _telemetryService?.PostProperties(TelemetryEventName.UpToDateCheckFail, new[]
                {
                    (TelemetryPropertyName.UpToDateCheck.FailReason, (object)reason),
                    (TelemetryPropertyName.UpToDateCheck.DurationMillis, _stopwatch.Elapsed.TotalMilliseconds),
                    (TelemetryPropertyName.UpToDateCheck.WaitDurationMillis, _waitTime.TotalMilliseconds),
                    (TelemetryPropertyName.UpToDateCheck.FileCount, _timestampCache.Count),
                    (TelemetryPropertyName.UpToDateCheck.ConfigurationCount, _upToDateCheckConfiguredInput.ImplicitInputs.Length),
                    (TelemetryPropertyName.UpToDateCheck.LogLevel, Level),
                    (TelemetryPropertyName.UpToDateCheck.Project, _projectGuid),
                    (TelemetryPropertyName.UpToDateCheck.CheckNumber, _checkNumber),
                    (TelemetryPropertyName.UpToDateCheck.IgnoreKinds, _ignoreKinds ?? ""),
                    (TelemetryPropertyName.UpToDateCheck.AccelerationResult, FileSystemOperations.AccelerationResult)
                });

                // Remember the failure reason and description for use in IncrementalBuildFailureDetector.
                // First, ensure times are converted to local time (in case logging is not enabled).
                ConvertToLocalTimes(values);
                FailureReason = reason;
                FailureDescription = string.Format(GetResourceString(resourceName), values);

                return false;
            }

            /// <summary>
            /// Publishes that the project is up-to-date via telemetry and the output window.
            /// </summary>
            public void UpToDate(int copyCount)
            {
                Assumes.Null(FailureReason);
                Assumes.Null(FailureDescription);
                Assumes.NotNull(FileSystemOperations);

                _stopwatch.Stop();

                _solutionBuildEventListener.NotifyProjectChecked(
                    upToDate: true,
                    buildAccelerationEnabled: FileSystemOperations.IsAccelerationEnabled,
                    result: FileSystemOperations.AccelerationResult,
                    copyCount: copyCount,
                    configurationCount: _upToDateCheckConfiguredInput.ImplicitInputs.Length,
                    fileCount: _timestampCache.Count,
                    waitTime: _waitTime,
                    checkTime: _stopwatch.Elapsed,
                    logLevel: Level);

                // Send telemetry.
                _telemetryService?.PostProperties(TelemetryEventName.UpToDateCheckSuccess, new[]
                {
                    (TelemetryPropertyName.UpToDateCheck.DurationMillis, (object)_stopwatch.Elapsed.TotalMilliseconds),
                    (TelemetryPropertyName.UpToDateCheck.WaitDurationMillis, _waitTime.TotalMilliseconds),
                    (TelemetryPropertyName.UpToDateCheck.FileCount, _timestampCache.Count),
                    (TelemetryPropertyName.UpToDateCheck.ConfigurationCount, _upToDateCheckConfiguredInput.ImplicitInputs.Length),
                    (TelemetryPropertyName.UpToDateCheck.LogLevel, Level),
                    (TelemetryPropertyName.UpToDateCheck.Project, _projectGuid),
                    (TelemetryPropertyName.UpToDateCheck.CheckNumber, _checkNumber),
                    (TelemetryPropertyName.UpToDateCheck.IgnoreKinds, _ignoreKinds ?? ""),
                    (TelemetryPropertyName.UpToDateCheck.AccelerationResult, FileSystemOperations.AccelerationResult),
                    (TelemetryPropertyName.UpToDateCheck.AcceleratedCopyCount, copyCount)
                });

                Info(nameof(VSResources.FUTD_UpToDate));

                System.Diagnostics.Debug.Assert(_checkers.Any(check => ReferenceEquals(check.Value, _check)), "Expected the current IBuildUpToDateCheckProvider to be present in the set of known checks for this configured project");

                if (_checkers.Count > 1 && Level >= LogLevel.Info)
                {
                    // There's more than one IBuildUpToDateCheckProvider for this configured project, so we are
                    // not the only up-to-date check. Other checks may also declare this project as out-of-date.
                    // When that happens, someone looking at the logs could be confused. The logs would say
                    // "Project is up to date", followed immediately by details of a build (assuming the other
                    // check doesn't log anything). To improve this situation, we log some information about
                    // other checks we detect that apply to the project, so that someone investigating this
                    // situation is not confused and knows where to move their investigation.
                    Info(nameof(VSResources.OtherUpToDateCheckProvidersPresent));

                    foreach (Lazy<IBuildUpToDateCheckProvider, IOrderPrecedenceMetadataView> check in _checkers)
                    {
                        if (!ReferenceEquals(check.Value, _check))
                        {
                            Info(nameof(VSResources.OtherUpToDateCheckProviderInfo_1), check.Value.GetType());
                        }
                    }
                }
            }

            public readonly struct Scope : IDisposable
            {
                private readonly Log _log;

                public Scope(Log log)
                {
                    _log = log;
                    _log.Indent++;
                }

                public void Dispose()
                {
                    _log.Indent--;
                }
            }
        }
    }
}
