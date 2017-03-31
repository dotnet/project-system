// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    /// Internal as we don't want anyone who depends on us to post events to our eventId.
    /// </summary>
    internal interface ITelemetryService
    {
        /// <summary>
        /// Posts a given telemetry event path to the telemetry service session for the program.
        /// </summary>
        void PostEvent(string telemetryEvent);

        /// <summary>
        /// Posts a given telemetry operation to the telemetry service. <seealso cref="TelemetrySessionExtensions.PostOperation(TelemetrySession, string, TelemetryResult, string, TelemetryEventCorrelation[])"/>
        /// </summary>
        /// <param name="operationPath">Postfix on "vs/projectsystem/"</param>
        /// <param name="Result">The result of the operation</param>
        /// <param name="resultSummary">Summary of the result</param>
        /// <param name="correlatedWith">Events to correlate this event with</param>
        /// <returns></returns>
        TelemetryEventCorrelation PostOperation(string operationPath, TelemetryResult Result, string resultSummary = null, TelemetryEventCorrelation[] correlatedWith = null);

        /// <summary>
        /// Reports a given non-fatal watson to the telemetry service. <seealso cref="TelemetrySessionExtensions.PostFault(TelemetrySession, string, string, Exception, Func{IFaultUtility, int}, TelemetryEventCorrelation[])"/>
        /// </summary>
        /// <param name="eventPostfix">Postfix on "vs/projectsystem/"</param>
        /// <param name="description">Description to include with the NFW</param>
        /// <param name="exception">Exception that caused the NFW</param>
        /// <param name="callback">Gathers information for the NFW</param>
        TelemetryEventCorrelation Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null);
    }
}
