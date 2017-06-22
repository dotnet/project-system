// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ITelemetryService))]
    internal class VsTelemetryService : ITelemetryService
    {
        private const string EventPrefix = "vs/projectsystem/managed/";
        private const string PropertyPrefix = "VS.ProjectSystem.Managed.";

        /// <summary>
        /// Post an event with the event name also with the corresponding Property name and Property value. This 
        /// event will be correlated with the Project Telemetry Correlation asset.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="propertyName">Property name to be reported.</param>
        /// <param name="propertyValue">Property value to be reported.</param>
        public void PostProperty(string eventName, string propertyName, string propertyValue)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));
            Requires.NotNullOrEmpty(propertyValue, nameof(propertyValue));

            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            telemetryEvent.Properties.Add(BuildPropertyName(eventName, propertyName), propertyValue);
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }
        /// <summary>
        /// Post an event with the event name also with the corresponding Property names and Property values. This 
        /// event will be correlated with the Project Telemetry Correlation asset.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">List of Property name and corresponding values. PropertyName and PropertyValue cannot be null or empty.</param>
        public void PostProperties(string eventName, IEnumerable<(string propertyName, string propertyValue)> properties)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(properties, nameof(properties));

            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            foreach (var property in properties)
            {
                telemetryEvent.Properties.Add(BuildPropertyName(eventName, property.propertyName), property.propertyValue);
            }

            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        public string HashValue(string value)
        {
            var inputBytes = Encoding.UTF8.GetBytes(value);
            var hashedBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(hashedBytes);
        }


        /// <summary>
        /// Build a fully qualified property name based on it's parent event and the property name
        /// </summary>
        /// <param name="eventName">Name of the parent event.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Fully qualified property name.</returns>
        /// <remarks>
        /// Properties are expected to be in the following format - EventName.PropertyName
        /// with the the slashes from the vent name replaced by periods.
        /// e.g. vs/myevent would translate to VS.MyEvent.MyProperty
        /// </remarks>
        private string BuildPropertyName(string eventName, string propertyName)
        {
            string name = PropertyPrefix + eventName.Replace('/', '.');

            if (!name.EndsWith("."))
            {
                name += ".";
            }

            name += propertyName;

            return name;
        }
    }
}
