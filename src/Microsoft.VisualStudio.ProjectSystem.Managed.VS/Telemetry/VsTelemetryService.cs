// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ITelemetryService))]
    internal class VsTelemetryService : ITelemetryService
    {
        const string EventPrefix = "vs/projectsystem/";

        public void PostEvent(string telemetryEvent)
        {
            TelemetryService.DefaultSession.PostEvent($"{EventPrefix}{telemetryEvent}");
        }

        public TelemetryEventCorrelation PostOperation(string operationPath, TelemetryResult result, string resultSummary = null, TelemetryEventCorrelation[] correlatedWith = null)
        {
            return TelemetryService.DefaultSession.PostOperation(
                eventName: $"{EventPrefix}{operationPath}",
                result: result,
                resultSummary: resultSummary,
                correlatedWith: correlatedWith);
        }

        public TelemetryEventCorrelation Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null)
        {
            return TelemetryService.DefaultSession.PostFault(
                eventName: $"{EventPrefix}{eventPostfix}",
                description: description,
                exceptionObject: exception,
                gatherEventDetails: callback);
        }
    }
}
