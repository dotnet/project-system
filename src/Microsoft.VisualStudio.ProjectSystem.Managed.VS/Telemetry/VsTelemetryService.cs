﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ITelemetryService))]
    internal class VsTelemetryService : ITelemetryService
    {
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
                if (propertyValue is ComplexPropertyValue complexProperty)
                {
                    telemetryEvent.Properties.Add(propertyName, new TelemetryComplexProperty(complexProperty.Data));
                }
                else
                {
                    telemetryEvent.Properties.Add(propertyName, propertyValue);
                }
            }

            PostTelemetryEvent(telemetryEvent);
        }

        private void PostTelemetryEvent(TelemetryEvent telemetryEvent)
        {
#if DEBUG
            Assumes.True(telemetryEvent.Name.StartsWith(TelemetryEventName.Prefix, StringComparisons.TelemetryEventNames));

            foreach (string propertyName in telemetryEvent.Properties.Keys)
            {
                Assumes.True(propertyName.StartsWith(TelemetryPropertyName.Prefix, StringComparisons.TelemetryEventNames));
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
            byte[] inputBytes = Encoding.UTF8.GetBytes(value);
            using var cryptoServiceProvider = new SHA256CryptoServiceProvider();
            return BitConverter.ToString(cryptoServiceProvider.ComputeHash(inputBytes));
        }

        public string GetProjectId(UnconfiguredProject project)
        {
            string? fullPath = project?.FullPath;
            return Strings.IsNullOrEmpty(fullPath) ? string.Empty : HashValue(fullPath);
        }
    }
}
