// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Telemetry
{
    interface ITelemetryService
    {
        void PostEvent(TelemetryEvent telemetryEvent);

        void Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null);
    }
}
