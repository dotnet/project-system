// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.Telemetry
{
    internal sealed class DesignTimeTelemetryLogger : ILogger
    {
        private readonly ITelemetryService _telemetryService;

        private readonly Dictionary<int, TargetRecord> _targets = new();
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
                (TelemetryPropertyName.DesignTimeBuildComplete.Succeeded, (object)_succeeded),
                (TelemetryPropertyName.DesignTimeBuildComplete.Targets, targetResults)
            });
        }
    }
}
