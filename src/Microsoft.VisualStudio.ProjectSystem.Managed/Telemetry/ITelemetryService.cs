// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Telemetry
{
    internal interface ITelemetryService
    {
        Task PostEventAsync(string eventName, UnconfiguredProject unconfiguredProject, IUnconfiguredProjectCommonServices unconfiguredProjectCommonServices, IEnumerable<KeyValuePair<string, object>> properties);
        void PostProperty(string eventName, string propertyName, string propertyValue, Guid projectId, UnconfiguredProject unconfiguredProject);
        void PostProperties(string eventName, List<(string propertyName, string propertyValue)> properties, Guid projectId, UnconfiguredProject unconfiguredProject);
        string HashValue(string value);
    }
}