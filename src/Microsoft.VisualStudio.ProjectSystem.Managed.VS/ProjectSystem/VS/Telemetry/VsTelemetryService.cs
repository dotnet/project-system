// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Telemetry
{
    [Export(typeof(ITelemetryService))]
    internal class VsTelemetryService : ITelemetryService
    {
        public void PostEvent(TelemetryEvent telemetryEvent)
        {
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        public void Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null)
        {
            TelemetryService.DefaultSession.PostFault(
                eventName: $"vs/projectsystem/{eventPostfix}",
                description: description,
                exceptionObject: exception,
                gatherEventDetails: callback);
        }
    }
}
