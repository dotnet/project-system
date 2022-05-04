// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IBuildLoggerProviderAsync))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class DesignTimeTelemetryLoggerProvider : IBuildLoggerProviderAsync
    {
        [Import]
        private ITelemetryService? TelemetryService { get; set; }

        public Task<IImmutableSet<ILogger>> GetLoggersAsync(IReadOnlyList<string> targets, IImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
        {
            IImmutableSet<ILogger> loggers = ImmutableHashSet<ILogger>.Empty;

            if (properties.GetBoolProperty("DesignTimeBuild") == true)
            {
                Assumes.Present(TelemetryService);

                loggers = loggers.Add(new DesignTimeTelemetryLogger(TelemetryService));
            }

            return Task.FromResult(loggers);
        }
    }
}
