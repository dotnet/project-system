// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    internal interface ITelemetryService
    {
        Task PostEventAsync(string eventName, UnconfiguredProject unconfiguredProject, IUnconfiguredProjectCommonServices unconfiguredProjectCommonServices, IEnumerable<KeyValuePair<string, object>> properties);
        void PostProperty(string eventName, string propertyName, string propertyValue, UnconfiguredProject unconfiguredProject);
        void PostProperties(string eventName, List<(string propertyName, string propertyValue)> properties, UnconfiguredProject unconfiguredProject);
        string HashValue(string value);
    }
}