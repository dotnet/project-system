// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Telemetry
{
    internal interface ITelemetryService
    {
        /// <summary>
        /// Post an event with the event name.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        void PostEvent(string eventName);

        /// <summary>
        /// Post an event with the event name also with the corresponding Property name and Property value.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="propertyName">Property name to be reported.</param>
        /// <param name="propertyValue">Property value to be reported.</param>
        void PostProperty(string eventName, string propertyName, object propertyValue);

        /// <summary>
        /// Post an event with the event name also with the corresponding Property names and Property values.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">List of Property name and corresponding values. PropertyName and PropertyValue cannot be null or empty.</param>
        void PostProperties(string eventName, IEnumerable<(string propertyName, object propertyValue)> properties);

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        string HashValue(string value);
    }
}
