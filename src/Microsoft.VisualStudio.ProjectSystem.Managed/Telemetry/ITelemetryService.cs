// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Telemetry
{
    internal interface ITelemetryService
    {
        void PostEvent(string eventName);
        void PostProperty(string eventName, string propertyName, string propertyValue);
        void PostProperties(string eventName, IEnumerable<(string propertyName, string propertyValue)> properties);
        string HashValue(string value);
    }
}
