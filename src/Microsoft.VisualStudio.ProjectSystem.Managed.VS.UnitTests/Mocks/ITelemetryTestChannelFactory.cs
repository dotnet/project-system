// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.Telemetry
{
    internal static class ITelemetryTestChannelFactory
    {
        public static ITelemetryTestChannel ImplementOnPostEvent(Action<TelemetryEvent> action)
        {
            void callback(object sender, TelemetryTestChannelEventArgs e) 
            {
                if (ShouldIgnoreEvent(e.Event))
                    return;

                action(e.Event);
            }

            var mock = new Mock<ITelemetryTestChannel>();

            mock.Setup(m => m.OnPostEvent(It.IsAny<object>(), It.IsAny<TelemetryTestChannelEventArgs>()))
                .Callback((Action<object, TelemetryTestChannelEventArgs>)callback);

            return mock.Object;
        }

        private static bool ShouldIgnoreEvent(TelemetryEvent telemetryEvent)
        {
            return !telemetryEvent.Name.StartsWith(TelemetryEventName.Prefix);
        }
    }
}
