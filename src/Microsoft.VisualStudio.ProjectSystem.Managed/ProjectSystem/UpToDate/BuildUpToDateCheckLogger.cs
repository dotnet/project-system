// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed class BuildUpToDateCheckLogger
    {
        private readonly TextWriter? _logger;
        private readonly LogLevel _requestedLogLevel;
        private readonly string _fileName;
        private readonly ITelemetryService _telemetryService;

        public BuildUpToDateCheckLogger(TextWriter? logger, LogLevel requestedLogLevel, string projectPath, ITelemetryService telemetryService)
        {
            _logger = logger;
            _requestedLogLevel = requestedLogLevel;
            _telemetryService = telemetryService;
            _fileName = Path.GetFileNameWithoutExtension(projectPath);
        }

        private void Log(LogLevel level, string message, params object[] values)
        {
            if (level <= _requestedLogLevel)
            {
                // These are user visible, so we want them in local times so that 
                // they correspond with dates/times that Explorer, etc shows
                ConvertToLocalTimes(values);

                _logger?.WriteLine($"FastUpToDate: {string.Format(message, values)} ({_fileName})");
            }
        }

        private static void ConvertToLocalTimes(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is DateTime time)
                {
                    values[i] = time.ToLocalTime();
                }
            }
        }

        public void Minimal(string message, params object[] values) => Log(LogLevel.Minimal, message, values);
        public void Info(string message, params object[] values) => Log(LogLevel.Info, message, values);
        public void Verbose(string message, params object[] values) => Log(LogLevel.Verbose, message, values);

        public bool Fail(string reason, string message, params object[] values)
        {
            Minimal(message, values);
            _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, reason);
            return false;
        }

        public void UpToDate()
        {
            _telemetryService.PostEvent(TelemetryEventName.UpToDateCheckSuccess);
            Info("Project is up to date.");
        }
    }
}
