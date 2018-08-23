// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.VisualStudio.Telemetry
{
    public class TelemetryTestContext : IDisposable
    {
        private readonly ITelemetryTestChannel _channel;

        public TelemetryTestContext(ITelemetryTestChannel channel)
        {
            _channel = channel;

            TelemetryService.DefaultSession.IsOptedIn = true;
            TelemetryService.DefaultSession.Start();
            TelemetryService.AttachTestChannel(channel);
        }

        public void Dispose()
        {
            TelemetryService.DefaultSession.Dispose();
            TelemetryService.DetachTestChannel(_channel);

            // WORKAROUND: There's no public way to force the "default session" to publish events without disposing it and 
            // disposing it causes it to be disposed for the lifetime of the AppDomain. As a workaround. we reset the 
            // internal property to null,which just causes it to be recreated the next time the property is accessed.

            PropertyInfo property = typeof(TelemetryService).GetProperty("InternalDefaultSession", BindingFlags.Static | BindingFlags.NonPublic);
            property.SetValue(null, null);
        }
    }
}
