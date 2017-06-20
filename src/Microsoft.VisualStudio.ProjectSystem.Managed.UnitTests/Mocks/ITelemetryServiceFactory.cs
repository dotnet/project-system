// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Telemetry;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class ITelemetryServiceFactory
    {
        public static ITelemetryService Implement(
            Action<string> postEvent = null,
            Action<Tuple<string, string, string>> postProperty = null,
            Action<Tuple<string, List<(string propertyName, string propertyValue)>>> postProperties = null)
        {
            var service = new Mock<ITelemetryService>();
            if (postEvent != null)
            {
                service.Setup(s => s.PostEvent(It.IsAny<string>()))
                       .Callback<string>((e) => postEvent(e));
            }

            if (postProperty != null)
            {
                service.Setup(s => s.PostProperty(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .Callback<string, string, string>((e, p , v) => postProperty(Tuple.Create(e, p ,v)));
            }

            if (postProperties != null)
            {
                service.Setup(s => s.PostProperties(It.IsAny<string>(), It.IsAny<List<(string propertyName, string propertyValue)>>()))
                       .Callback<string, List<(string propertyName, string propertyValue)>>((e, p) => postProperties(Tuple.Create(e, p)));
            }

            service.Setup(s => s.HashValue(It.IsAny<string>()))
                    .Returns<string>(v => v + "#Hashed");

            return service.Object;
        }
    }
}
