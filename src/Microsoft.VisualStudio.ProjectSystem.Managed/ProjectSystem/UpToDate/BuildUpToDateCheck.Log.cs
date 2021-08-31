// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        private sealed class Log
        {
            private readonly TextWriter _writer;
            private readonly Stopwatch _stopwatch;
            private readonly TimestampCache _timestampCache;
            private readonly string _fileName;
            private readonly ITelemetryService _telemetryService;
            private readonly UpToDateCheckConfiguredInput _upToDateCheckConfiguredInput;

            public LogLevel Level { get; }

            public Log(TextWriter writer, LogLevel requestedLogLevel, Stopwatch stopwatch, TimestampCache timestampCache, string projectPath, ITelemetryService telemetryService, UpToDateCheckConfiguredInput upToDateCheckConfiguredInput)
            {
                _writer = writer;
                Level = requestedLogLevel;
                _stopwatch = stopwatch;
                _timestampCache = timestampCache;
                _telemetryService = telemetryService;
                _upToDateCheckConfiguredInput = upToDateCheckConfiguredInput;
                _fileName = Path.GetFileNameWithoutExtension(projectPath);
            }

            private void Write(LogLevel level, string message, object arg0)
            {
                if (level <= Level)
                {
                    // These are user visible, so we want them in local times so that 
                    // they correspond with dates/times that Explorer, etc shows
                    ConvertToLocalTime(ref arg0);

                    _writer.WriteLine($"FastUpToDate: {string.Format(message, arg0)} ({_fileName})");
                }
            }

            private void Write(LogLevel level, string message, object arg0, object arg1)
            {
                if (level <= Level)
                {
                    // These are user visible, so we want them in local times so that 
                    // they correspond with dates/times that Explorer, etc shows
                    ConvertToLocalTime(ref arg0);
                    ConvertToLocalTime(ref arg1);

                    _writer.WriteLine($"FastUpToDate: {string.Format(message, arg0, arg1)} ({_fileName})");
                }
            }

            private void Write(LogLevel level, string message, params object[] values)
            {
                if (level <= Level)
                {
                    // These are user visible, so we want them in local times so that 
                    // they correspond with dates/times that Explorer, etc shows
                    ConvertToLocalTimes(values);

                    _writer.WriteLine($"FastUpToDate: {string.Format(message, values)} ({_fileName})");
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
                    value = time.ToLocalTime();
                }
            }

            public void Minimal(string message, params object[] values) => Write(LogLevel.Minimal, message, values);
            public void Info(string message, object arg0) => Write(LogLevel.Info, message, arg0);
            public void Info(string message, object arg0, object arg1) => Write(LogLevel.Info, message, arg0, arg1);
            public void Info(string message, params object[] values) => Write(LogLevel.Info, message, values);
            public void Verbose(string message, object arg0) => Write(LogLevel.Verbose, message, arg0);
            public void Verbose(string message, params object[] values) => Write(LogLevel.Verbose, message, values);

            public bool Fail(string reason, string message, params object[] values)
            {
                _stopwatch.Stop();

                Minimal(message, values);

                _telemetryService.PostProperties(TelemetryEventName.UpToDateCheckFail, new[]
                {
                    (TelemetryPropertyName.UpToDateCheckFailReason, (object)reason),
                    (TelemetryPropertyName.UpToDateCheckDurationMillis, _stopwatch.Elapsed.TotalMilliseconds),
                    (TelemetryPropertyName.UpToDateCheckFileCount, _timestampCache.Count),
                    (TelemetryPropertyName.UpToDateCheckConfigurationCount, _upToDateCheckConfiguredInput.ImplicitInputs.Length)
                });

                return false;
            }

            public void UpToDate()
            {
                _stopwatch.Stop();

                _telemetryService.PostProperties(TelemetryEventName.UpToDateCheckSuccess, new[]
                {
                    (TelemetryPropertyName.UpToDateCheckDurationMillis, (object)_stopwatch.Elapsed.TotalMilliseconds),
                    (TelemetryPropertyName.UpToDateCheckFileCount, _timestampCache.Count),
                    (TelemetryPropertyName.UpToDateCheckConfigurationCount, _upToDateCheckConfiguredInput.ImplicitInputs.Length)
                });

                Info("Project is up to date.");
            }
        }
    }
}
