// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Task = System.Threading.Tasks.Task;

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
