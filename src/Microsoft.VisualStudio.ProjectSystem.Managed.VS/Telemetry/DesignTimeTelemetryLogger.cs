// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.Telemetry
{
    internal sealed class DesignTimeTelemetryLogger : ILogger
    {
        private readonly ITelemetryService _telemetryService;

        private readonly Dictionary<int, TargetRecord> _targets = new Dictionary<int, TargetRecord>();
        private bool _succeeded;

        public DesignTimeTelemetryLogger(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        public LoggerVerbosity Verbosity { get; set; }

        public string? Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.TargetStarted += TargetStarted;
            eventSource.TargetFinished += TargetFinished;
            eventSource.BuildFinished += BuildFinished;
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            _succeeded = e.Succeeded;
        }

        private void TargetStarted(object sender, TargetStartedEventArgs e)
        {
            _targets[e.BuildEventContext.TargetId] = new TargetRecord(e.TargetName, e.Timestamp);
        }

        private void TargetFinished(object sender, TargetFinishedEventArgs e)
        {
            if (_targets.TryGetValue(e.BuildEventContext.TargetId, out TargetRecord record))
            {
                record.Ended = e.Timestamp;
            }
        }

        public void Shutdown()
        {
            var builder = PooledStringBuilder.GetInstance();

            foreach (TargetRecord target in _targets.Values
                .Where(v => v.Elapsed != TimeSpan.Zero)
                .OrderByDescending(v => v.Elapsed)
                .Take(10))
            {
                builder.Append(_telemetryService.HashValue(target.TargetName));
                builder.Append(':');
                builder.Append(target.Elapsed.TotalMilliseconds);
                builder.Append(';');
            }

            string targetResults = builder.ToStringAndFree();

            _telemetryService.PostProperties(TelemetryEventName.DesignTimeBuildComplete, new[]
            {
                (TelemetryPropertyName.DesignTimeBuildCompleteSucceeded, (object)_succeeded),
                (TelemetryPropertyName.DesignTimeBuildCompleteTargets, targetResults)
            });
        }
    }
}
