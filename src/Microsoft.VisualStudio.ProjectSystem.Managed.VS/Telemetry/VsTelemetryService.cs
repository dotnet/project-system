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
        public bool PostFault(string eventName, Exception exceptionObject)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNull(exceptionObject, nameof(exceptionObject));

            var faultEvent = new FaultEvent(eventName,
                                            description: null,
                                            exceptionObject);

            PostTelemetryEvent(faultEvent);

            return true;
        }


        public void PostEvent(string eventName)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));

            PostTelemetryEvent(new TelemetryEvent(eventName));
        }

        public void PostProperty(string eventName, string propertyName, object propertyValue)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));
            Requires.NotNull(propertyValue, nameof(propertyValue));

            var telemetryEvent = new TelemetryEvent(eventName);
            telemetryEvent.Properties.Add(propertyName, propertyValue);

            PostTelemetryEvent(telemetryEvent);
        }

        public void PostProperties(string eventName, IEnumerable<(string propertyName, object propertyValue)> properties)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(properties, nameof(properties));

            var telemetryEvent = new TelemetryEvent(eventName);
            foreach ((string propertyName, object propertyValue) in properties)
            {
                telemetryEvent.Properties.Add(propertyName, propertyValue);
            }

            PostTelemetryEvent(telemetryEvent);
        }

        private void PostTelemetryEvent(TelemetryEvent telemetryEvent)
        {
#if DEBUG
            Assumes.True(telemetryEvent.Name.StartsWith(TelemetryEventName.Prefix, StringComparison.Ordinal));

            foreach (string propertyName in telemetryEvent.Properties.Keys)
            {
                Assumes.True(propertyName.StartsWith(TelemetryPropertyName.Prefix, StringComparison.Ordinal));
            }
#endif

            PostEventToSession(telemetryEvent);
        }

        protected virtual void PostEventToSession(TelemetryEvent telemetryEvent)
        {
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        public string HashValue(string value)
        {
            // Don't hash PII for internal users since we don't need to.
            if (TelemetryService.DefaultSession.IsUserMicrosoftInternal)
            {
                return value;
            }

            byte[] inputBytes = Encoding.UTF8.GetBytes(value);
            using (var cryptoServiceProvider = new SHA256CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoServiceProvider.ComputeHash(inputBytes));
            }
        }
    }
}
