// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.Telemetry
{
    internal static class IConfiguredProjectPackageRestoreTelemetryServiceFactory
    {
        public static IConfiguredProjectPackageRestoreTelemetryService Create()
        {
            var mock = new Mock<IConfiguredProjectPackageRestoreTelemetryService>();

            return mock.Object;
        }
    }
}
