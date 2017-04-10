// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ITelemetryService))]
    internal class VsTelemetryService : ITelemetryService
    {
        public void PostEvent(string eventName)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));

            TelemetryService.DefaultSession.PostEvent(eventName);
        }

        public TelemetryEventCorrelation PostOperation(string eventName, TelemetryResult result, string resultSummary = null, TelemetryEventCorrelation[] correlatedWith = null)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            if (result == TelemetryResult.None)
                throw new ArgumentException(null, nameof(result));

            return TelemetryService.DefaultSession.PostOperation(
                eventName: eventName,
                result: result,
                resultSummary: resultSummary,
                correlatedWith: correlatedWith);
        }

        public TelemetryEventCorrelation Report(string eventName, string description, Exception exception, Func<IFaultUtility, int> callback = null)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNull(exception, nameof(exception));

            return TelemetryService.DefaultSession.PostFault(
                eventName: eventName,
                description: description,
                exceptionObject: exception,
                gatherEventDetails: callback);
        }
    }
}
