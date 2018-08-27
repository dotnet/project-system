// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Telemetry
{
    internal static class ITelemetryServiceFactory
    {
        public static ITelemetryService Create()
        {
            var mock = new Mock<ITelemetryService>();

            mock.Setup(s => s.PostFault(It.IsAny<string>(), It.IsAny<Exception>()))
                .Returns(true);

            return mock.Object;
        }
    }
}
