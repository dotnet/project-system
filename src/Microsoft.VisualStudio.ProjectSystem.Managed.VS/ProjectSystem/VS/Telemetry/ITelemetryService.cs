// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Telemetry
{
    /// <summary>
    /// Internal as we don't want anyone who depends on us to post events to our eventId.
    /// </summary>
    internal interface ITelemetryService
    {
        /// <summary>
        /// Posts a given telemetry event to the telemetry service session for the program.
        /// </summary>
        void PostEvent(TelemetryEvent telemetryEvent);

        /// <summary>
        /// Reports a given non-fatal watson to the telemetry service
        /// </summary>
        /// <param name="eventPostfix">Postfix on "vs/projectsystem/".</param>
        /// <param name="description">Description to include with the NFW</param>
        /// <param name="exception">Exception that caused the NFW</param>
        /// <param name="callback">Gathers information for the NFW</param>
        void Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null);
    }
}
