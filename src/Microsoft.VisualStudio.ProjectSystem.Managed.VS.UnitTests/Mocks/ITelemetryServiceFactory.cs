// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Moq;

namespace Microsoft.VisualStudio.Telemetry
{
    internal static class ITelemetryServiceFactory
    {
        public class TelemetryParameters
        {
            public string EventName { get; set; }

            public IEnumerable<(string propertyName, object propertyValue)> Properties { get; set; }
        }

        public static ITelemetryService Create() => Mock.Of<ITelemetryService>();

        public static ITelemetryService Create(TelemetryParameters callParameters)
        {
            var telemetryService = new Mock<ITelemetryService>();

            telemetryService.Setup(t => t.PostEvent(It.IsAny<string>()))
                .Callback((string name) => callParameters.EventName = name);

            telemetryService.Setup(t => t.PostProperty(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Callback((string e, string p, object v) =>
                {
                    callParameters.EventName = e;
                    callParameters.Properties = new List<(string, object)>
                    {
                        (p, v)
                    };
                });

            telemetryService.Setup(t => t.PostProperties(It.IsAny<string>(), It.IsAny<IEnumerable<(string propertyName, object propertyValue)>>()))
                .Callback((string e, IEnumerable<(string propertyName, object propertyValue)> p) =>
                {
                    callParameters.EventName = e;
                    callParameters.Properties = p;
                });

            return telemetryService.Object;
        }
    }
}
