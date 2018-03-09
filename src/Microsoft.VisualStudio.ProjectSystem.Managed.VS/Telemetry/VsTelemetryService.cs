// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<string, (string Event, ConcurrentDictionary<string, string> Properties)> _eventCache = new ConcurrentDictionary<string, (string, ConcurrentDictionary<string, string>)>();

        private (string eventPath, ConcurrentDictionary<string, string> properties) GetEventInfo(string eventName)
        {
            if (!_eventCache.TryGetValue(eventName, out var eventInfo))
            {
                eventInfo = (EventPrefix + eventName.ToLower(), new ConcurrentDictionary<string, string>());
                _eventCache[eventName] = eventInfo;
            }

            return eventInfo;
        }

        private string GetEventName(string eventName) => GetEventInfo(eventName).eventPath;

        private string GetPropertyName(string eventName, string propertyName)
        {
            (_, ConcurrentDictionary<string, string> properties) = GetEventInfo(eventName);
            if (!properties.TryGetValue(propertyName, out var fullPropertyName))
            {
                fullPropertyName = BuildPropertyName(eventName, propertyName);
                properties[propertyName] = fullPropertyName;
            }

            return fullPropertyName;
        }

        /// <summary>
        /// Post an event with the event name.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        public void PostEvent(string eventName)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));

            var telemetryEvent = new TelemetryEvent(GetEventName(eventName));
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        /// <summary>
        /// Post an event with the event name also with the corresponding Property name and Property value.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="propertyName">Property name to be reported.</param>
        /// <param name="propertyValue">Property value to be reported.</param>
        public void PostProperty(string eventName, string propertyName, object propertyValue)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));
            Requires.NotNull(propertyValue, nameof(propertyValue));

            var telemetryEvent = new TelemetryEvent(GetEventName(eventName));
            telemetryEvent.Properties.Add(BuildPropertyName(eventName, propertyName), propertyValue);
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        /// <summary>
        /// Post an event with the event name also with the corresponding Property names and Property values.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">List of Property name and corresponding values. PropertyName and PropertyValue cannot be null or empty.</param>
        public void PostProperties(string eventName, IEnumerable<(string propertyName, object propertyValue)> properties)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(properties, nameof(properties));

            var telemetryEvent = new TelemetryEvent(GetEventName(eventName));
            foreach ((string propertyName, object propertyValue) in properties)
            {
                telemetryEvent.Properties.Add(GetPropertyName(eventName, propertyName), propertyValue);
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
            // Don't hash PII for internal users since we don't need to.
            if (TelemetryService.DefaultSession.IsUserMicrosoftInternal)
            {
                return value;
            }

            var inputBytes = Encoding.UTF8.GetBytes(value);
            using (var cryptoServiceProvider = new SHA256CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoServiceProvider.ComputeHash(inputBytes));
            }
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
        private static string BuildPropertyName(string eventName, string propertyName)
        {
            var name = PropertyPrefix + eventName.Replace('/', '.');

            if (!name.EndsWith("."))
            {
                name += ".";
            }

            name += propertyName;

            return name;
        }
    }
}
