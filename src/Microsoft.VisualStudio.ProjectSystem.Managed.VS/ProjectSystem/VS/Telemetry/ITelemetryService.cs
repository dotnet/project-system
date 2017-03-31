// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Telemetry
{
    /// <summary>
    /// Internal as we don't want anyone who depends on us to post events to our eventId.
    /// </summary>
    internal interface ITelemetryService
    {
        /// <summary>
        /// Posts a given telemetry event path to the telemetry service session for the program.
        /// </summary>
        /// <param name="telemetryEvent">Name of the event to post.</param>
        void PostEvent(string telemetryEvent);

        /// <summary>
        /// Posts a given telemetry operation to the telemetry service. <seealso cref="TelemetrySessionExtensions.PostOperation(TelemetrySession, string, TelemetryResult, string, TelemetryEventCorrelation[])"/>
        /// </summary>
        /// <param name="operationPath">Postfix on "vs/projectsystem/"</param>
        /// <param name="result">The result of the operation</param>
        /// <param name="resultSummary">Summary of the result</param>
        /// <param name="correlatedWith">Events to correlate this event with</param>
        /// <returns>Posted telemetry event.</returns>
        TelemetryEventCorrelation PostOperation(string operationPath, TelemetryResult result, string resultSummary = null, TelemetryEventCorrelation[] correlatedWith = null);

        /// <summary>
        /// Reports a given non-fatal watson to the telemetry service. <seealso cref="TelemetrySessionExtensions.PostFault(TelemetrySession, string, string, Exception, Func{IFaultUtility, int}, TelemetryEventCorrelation[])"/>
        /// </summary>
        /// <param name="eventPostfix">Postfix on "vs/projectsystem/"</param>
        /// <param name="description">Description to include with the NFW</param>
        /// <param name="exception">Exception that caused the NFW</param>
        /// <param name="callback">Gathers information for the NFW</param>
        /// <returns>Posted telemetry event.</returns>
        TelemetryEventCorrelation Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null);

        /// <summary>
        /// Posts an event with a single property.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyValue">Value of the property.</param>
        /// <param name="unconfiguredProject">Correlated project asset for this event.</param>
        void PostProperty(string eventName, string propertyName, string propertyValue, UnconfiguredProject unconfiguredProject = null);

        /// <summary>
        /// Posts an event with multiple properties.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">Collection of property name and values.</param>
        /// <param name="unconfiguredProject">Correlated project asset for this event.</param>
        void PostProperties(string eventName, IEnumerable<(string propertyName, string propertyValue)> properties, UnconfiguredProject unconfiguredProject = null);

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        string HashValue(string value);
    }
}
